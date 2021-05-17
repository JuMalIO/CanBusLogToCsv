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
            var columns = new string[] { "Tick", "Id", "Length", "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "Description", "Calculations" };
            var csv = new StringBuilder();

            csv.AppendLine(string.Join(separator, columns));

            foreach (var logMessage in logMessages)
            {
                var description = descriptions.FirstOrDefault(x => x.Id == logMessage.Id);

                var columnValues = new string[] {
                    logMessage.Tick.ToString(),
                    "0x" + logMessage.Id.ToString("X3"),
                    logMessage.Length.ToString(),
                    "0x" + logMessage.Data[0].ToString("X2"),
                    "0x" + logMessage.Data[1].ToString("X2"),
                    "0x" + logMessage.Data[2].ToString("X2"),
                    "0x" + logMessage.Data[3].ToString("X2"),
                    "0x" + logMessage.Data[4].ToString("X2"),
                    "0x" + logMessage.Data[5].ToString("X2"),
                    "0x" + logMessage.Data[6].ToString("X2"),
                    "0x" + logMessage.Data[7].ToString("X2"),
                    description?.Name,
                    GetCalculations(description?.Expressions, logMessage.Data)
                };

                csv.AppendLine(string.Join(separator, columnValues));
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        static string GetCalculations(List<DescriptionExpression> descriptionExpressions, byte[] data)
        {
            if (descriptionExpressions == null || !descriptionExpressions.Any())
            {
                return null;
            }

            return string.Join("|", descriptionExpressions.Select(x => $"{x.Name}: {Evaluate(x.Expression, data)}"));
        }

        static string Evaluate(string expression, byte[] data)
        {
            if (expression == null)
            {
                return null;
            }

            var newExpression = expression
                .Replace("{B0}", data[0].ToString())
                .Replace("{B1}", data[1].ToString())
                .Replace("{B2}", data[2].ToString())
                .Replace("{B3}", data[3].ToString())
                .Replace("{B4}", data[4].ToString())
                .Replace("{B5}", data[5].ToString())
                .Replace("{B6}", data[6].ToString())
                .Replace("{B7}", data[7].ToString());

            return new DataTable().Compute(newExpression, "").ToString();
        }
    }
}
