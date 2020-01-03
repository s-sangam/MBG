using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using MBGenerator.Models;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using MBGenerator.avro;
using Avro.Generic;

namespace MBGenerator.Services
{
    public class KafkaListOfCloudOrders : ICloudOrders
    {
 
        IProducer<Null, imageRequest> _producer;
        Action<DeliveryReport<Null, imageRequest>> _handler;
        int _idCounter = 0;

        public KafkaListOfCloudOrders()
        {
            // build the producer

            var config = new ProducerConfig
            {
                BootstrapServers = "kafka-server1:9092,kafka-server2:9092"
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = "schema-registry:8081"
            };

            var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

            _handler = r =>
                Console.WriteLine(!r.Error.IsError
                    ? $"Delivered message to {r.TopicPartitionOffset}"
                    : $"Delivery Error: {r.Error.Reason}");

            _producer = new ProducerBuilder<Null, imageRequest>(config).SetValueSerializer(new AvroSerializer<imageRequest>(schemaRegistry)).Build();

        }

        public CloudOrder GetCloudOrderById(int Id)
        {
            var _orders = new CloudOrder();
            _orders.Description = "depricate this";
            _orders.Id = 1;
            return _orders;
        }

        public IEnumerable<CloudOrder> GetAllCloudOrders()
        {
            // clean this up later, but for now return an empty list.
            List<CloudOrder> _orders = new List<CloudOrder> { };
            
            return _orders;
        }

        public void SendImageRequest(int display_x , int display_y , double min_x, double min_y, double max_x, double max_y, int depth, string connectionId)
        {
            var _request = new avro.imageRequest();
            _request.display_x = display_x;
            _request.display_y = display_y;
            _request.min_x = min_x;
            _request.min_y = min_y;
            _request.max_x = max_x;
            _request.max_y = max_y;
            _request.depth = depth;
            _request.connectionId = connectionId;

            _producer.ProduceAsync("imageReq", new Message<Null, imageRequest> { Value = _request });
            _producer.Flush();

            Console.WriteLine($"Sent image build request for {_request.display_x} - {_request.display_y} from connection {_request.connectionId}");
        }
    }
}
