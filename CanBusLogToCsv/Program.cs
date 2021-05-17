using CanBusLogToCsv.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CanBusLogToCsv
{
    class Program
    {
        static void Main(string[] args)
        {
            var descriptionsFile = File.ReadAllText("descriptions.json");
            var descriptions = JsonSerializer.Deserialize<List<Description>>(descriptionsFile);

            foreach (var arg in args)
            {
                var logMessages = ReadLogMessages(arg);

                logMessages.ForEach(x => x.Id = (ushort)(((x.Id & 0xFF) << 8) + ((x.Id >> 8) & 0xFF)));

                WriteCsv(arg + ".csv", logMessages, descriptions);
            }
        }

        static List<LogMessage> ReadLogMessages(string filePath)
        {
            var logMessages = new List<LogMessage>();

            using (var binaryReader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                while (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position >= LogMessage.Size)
                {
                    logMessages.Add(new LogMessage
                    {
                        Tick = binaryReader.ReadUInt32(),
                        Id = binaryReader.ReadUInt16(),
                        Length = binaryReader.ReadByte(),
                        Data = binaryReader.ReadBytes(LogMessage.MaxDataLength),
                        Empty = binaryReader.ReadByte()
                    });
                }
            }

            return logMessages;
        }

        static void WriteCsv(string filePath, List<LogMessage> logMessages, List<Description> descriptions)
        {
            const char separator = ';';
            var csv = new StringBuilder();
            var dataColumns = new string[LogMessage.MaxDataLength];
            var expressionNames = descriptions.SelectMany(x => x.Expressions).Select(x => x.Name).Distinct();

            var columns = new List<string> { "Tick", "Id", "Description", "Length" };
            columns.AddRange(dataColumns.Select((_, i) => $"B{i}"));
            columns.AddRange(expressionNames);

            csv.AppendLine(string.Join(separator, columns));

            foreach (var logMessage in logMessages)
            {
                var description = descriptions.FirstOrDefault(x => x.Id == logMessage.Id);

                var columnValues = new List<string> { logMessage.Tick.ToString(), "0x" + logMessage.Id.ToString("X3"), description?.Name, logMessage.Length.ToString() };
                columnValues.AddRange(dataColumns.Select((_, i) => i < logMessage.Length ? "0x" + logMessage.Data[i].ToString("X2") : null));
                columnValues.AddRange(expressionNames.Select(x => GetExpressionValue(x, description?.Expressions, logMessage.Data)));
                
                csv.AppendLine(string.Join(separator, columnValues));
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        static string GetExpressionValue(string expressionName, List<DescriptionExpression> descriptionExpressions, byte[] data)
        {
            if (descriptionExpressions == null || !descriptionExpressions.Any())
            {
                return null;
            }

            return Evaluate(descriptionExpressions.FirstOrDefault(x => x.Name == expressionName)?.Expression, data);
        }

        static string Evaluate(string expression, byte[] data)
        {
            if (expression == null)
            {
                return null;
            }

            var newExpression = new StringBuilder();
            int startIndex;
            while ((startIndex = expression.IndexOf("{B")) >= 0)
            {
                newExpression.Append(expression.Substring(0, startIndex));
                startIndex += 2;
                var endIndex = expression.IndexOf("}");
                var bits = expression.Substring(startIndex, endIndex - startIndex).Split(":");
                var value = bits.Length == 3
                    ? ((data[int.Parse(bits[0])]) >> (int.Parse(bits[1]))) & ((1 << int.Parse(bits[2])) - 1)
                    : data[int.Parse(bits[0])];
                newExpression.Append(value);
                endIndex += 1;
                expression = expression.Substring(endIndex);
            }
            newExpression.Append(expression);

            return new DataTable().Compute(newExpression.ToString(), "").ToString();
        }
    }
}
