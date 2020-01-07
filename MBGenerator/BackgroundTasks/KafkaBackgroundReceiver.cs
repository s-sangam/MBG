using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Confluent.Kafka;
using MBGenerator.avro;
using Confluent.SchemaRegistry.Serdes;
using Confluent.SchemaRegistry;
using Confluent.Kafka.SyncOverAsync;

namespace MBGenerator.BackgroundTasks
{
    public class KafkaBackgroundReceiver : BackgroundService
    {

        private IConsumer<Ignore, imageResponse> _consumer;
        private Microsoft.AspNetCore.SignalR.IHubContext<ImageMarshallingHub> _imageHub;

        public KafkaBackgroundReceiver(Microsoft.AspNetCore.SignalR.IHubContext<ImageMarshallingHub> imageHub)
        {
            // build the kafka consumer

            _imageHub = imageHub;

            var conf = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = "kafka-server1:9092",
                // Note: The AutoOffsetReset property detemines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = "kafka-schema-registry:8081"
            };

            var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

            //_consumer = new ConsumerBuilder<Ignore, string>(conf).Build();
            _consumer = new ConsumerBuilder<Ignore, imageResponse>(conf).SetValueDeserializer(new AvroDeserializer<imageResponse>(schemaRegistry).AsSyncOverAsync<imageResponse>()).Build();
            _consumer.Subscribe("imageResponse");

        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                
                // Have a look and see if there are any messages coming back from the 
                // Image Builder which we would then forward on to the relevant client webpage.

                try
                {
                    var cr = _consumer.Consume(new TimeSpan(0));
                    if (cr != null)
                    {
                        //TODO: Call the SignalR hub to send this back to the client
                        Console.WriteLine($"Received message {cr.Value}");
                        try
                        {
                            // send a message to connected clients...
                            
                            await _imageHub.Clients.Client(cr.Value.connectionId).SendCoreAsync("RecieveURL", new object[] { cr.Value.url, cr.Value.display_x, cr.Value.display_y });
                            

                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Could not find the signalR hub {e.Error.Reason}");
                        }
                    }
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"Error occured: {e.Error.Reason}");
                }
             
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
