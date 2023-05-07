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
            int randomNumber = 10;// rnd.Next(1, 11);
            message.MessageId=Guid.NewGuid().ToString();
            message.DeviceId = $"machine-{randomNumber}";
            message.Status = randomNumber % 2 == 0 ? "Running" : "Stopped";
            message.Timestamp = new DateTime(2023,05,05).ToUniversalTime().ToTimestamp();
            message.Temperature = 88;//message.Status == "Running" ? rnd.Next(50, 99) : rnd.Next(10, 30);
            message.EnergyConsumption = 169;// message.Status == "Running" ? rnd.Next(100, 200) : 0;
            message.ProductionRate = 10;
            // the other properties of this message form 80.4965 bytes on the wire.
            var additionalBytes =(int)Math.Round((payloadSize - 80.4965) / 0.5005); //this is the formula to get exactly the payloadsize passed in the protobuf wire data
            IEnumerable<char> list = CreatePayload(additionalBytes);
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
