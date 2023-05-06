using CommonModule.Protos;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTClient.gRPC
{
    internal class DataGenerator
    {
        public static Random rnd= new Random();
        /// <summary>
        /// This method generates data depending on the number of bits passed.
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <returns></returns>
        public static EquipmentMessage GenerateData(int payloadSize)
        {
            var message = new EquipmentMessage();
            int randomNumber = rnd.Next(1, 11);
            message.MessageId=Guid.NewGuid().ToString();
            message.DeviceId = $"machine-{randomNumber}";
            message.Status = randomNumber % 2 == 0 ? "Running" : "Stopped";
            message.Timestamp = DateTime.UtcNow.ToTimestamp();
            message.Temperature = message.Status == "Running" ? rnd.Next(50, 100) : rnd.Next(10, 30);
            message.EnergyConsumption = message.Status == "Running" ? rnd.Next(100, 200) : 0;
            message.ProductionRate = 10;
            IEnumerable<char> list = CreatePayload(payloadSize);
            message.Payload= string.Join("", list);
            return message;
        }
        /// <summary>
        /// This method creates data based on the payload size
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <returns></returns>
        private static IEnumerable<char> CreatePayload(int payloadSize)
        {
            // 1 kb =1024 bytes and 1 byte = 8 bits
            int numberOfBits = payloadSize * 8;
            // 1 character is 16 bits
            int numberofChars = numberOfBits / 16;
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var list = Enumerable.Repeat(0, numberofChars).Select(x => chars[random.Next(chars.Length)]);
            return list;
        }
    }
}
