using System;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using MBGenerator.avro;

namespace MBGenerator.Services
{
    public class KafkaListOfCloudOrders : ICloudOrders
    {
 
        IProducer<Null, imageRequest> _producer;
        Action<DeliveryReport<Null, imageRequest>> _handler;

        public KafkaListOfCloudOrders()
        {
            // build the producer

            var config = new ProducerConfig
            {
                BootstrapServers = "kafka-server1:9092"
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = "kafka-schema-registry:8081"
            };

            var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

            _handler = r =>
                Console.WriteLine(!r.Error.IsError
                    ? $"Delivered message to {r.TopicPartitionOffset}"
                    : $"Delivery Error: {r.Error.Reason}");

            _producer = new ProducerBuilder<Null, imageRequest>(config).SetValueSerializer(new AvroSerializer<imageRequest>(schemaRegistry)).Build();

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
