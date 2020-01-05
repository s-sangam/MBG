using System;
using System.Threading;
using Confluent.Kafka;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MBGenerator.avro;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Confluent.Kafka.SyncOverAsync;
using System.Threading.Tasks;

namespace MBImageBuilder
{
    class Program
    {
        static void Main(string[] args)
        {

            // #if (DEBUG)
#if (false)
            {
                // if we're in debug mode, construct an imageRequest object from the args and write the output to files
                imageRequest _request = new imageRequest();
                _request.depth = 0;

                double min_x = -2;
                double max_x = 2;
                double min_y = -2;
                double max_y = 2;

                double step_x = (max_x - min_x) / 10;
                double step_y = (max_y - min_y) / 10;

                // build an 10x10 array of images given the rectangle range specified in the args.
                var builder = new MBImageBuilder(true);

                for (int x = 0; x < 10; x++)
                {
                    _request.display_x = x;
                    _request.min_x = min_x + (x * step_x);
                    _request.max_x = min_x + ((x + 1) * step_x);

                    for (int y = 0; y < 10; y++)
                    {
                        _request.display_y = y;
                        _request.min_y = min_y + (y * step_y);
                        _request.max_y = min_y + ((y + 1) * step_y);

                        var filename = builder.MBCreateImage(_request);
                        Console.WriteLine($"created file {filename}");
                    }
                }
            }
#endif

            // we're in service mode - set up connections to mongodb & kafka and wait for requests.

            // create a connection to mongo for storing messages, and create a GridFS bucket for storing computed images.
            try
            {


                MongoCredential credential = MongoCredential.CreateCredential("MBImageDatabase", "mongo_user", "password123");

                var settings = new MongoClientSettings
                {
                    Credential = credential,
                    Server = new MongoServerAddress("mongodb", 27017),
                    UseTls = false,
                };

                var mongoClient = new MongoClient(settings);

                IMongoDatabase db = mongoClient.GetDatabase("MBImageDatabase");
                IMongoCollection<BsonDocument> buildRequestCollection = db.GetCollection<BsonDocument>("BuildRequests");
                IGridFSBucket bucket = new GridFSBucket(db);

                // build the kafka consumer (to receive requests to build an image). use the imageRequest serializer.  

                IConsumer<Ignore, imageRequest> _consumer;

                try
                {
                    var conf = new ConsumerConfig
                    {
                        GroupId = "test-consumer-group",
                        BootstrapServers = "kafka-server1:9092,kafka-server2:9092",
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    };

                    var schemaRegistryConfig = new SchemaRegistryConfig
                    {
                        Url = "schema-registry:8081"
                    };

                    var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

                    //create the consumer. note the hack to turn the asnyc deserialiser into a sync one. seems to be something confluent are changing...
                    _consumer = new ConsumerBuilder<Ignore, imageRequest>(conf).SetValueDeserializer(new AvroDeserializer<imageRequest>(schemaRegistry).AsSyncOverAsync<imageRequest>()).Build();
                    _consumer.Subscribe("imageReq");

                    //build the kafka publisher (to send back the Id of created images to the client)
                    try
                    {

                        IProducer<Null, imageResponse> _producer;
                        Action<DeliveryReport<Null, string>> _handler;

                        var config = new ProducerConfig
                        {
                            BootstrapServers = "kafka-server1:9092,kafka-server2:9092"
                        };

                        _handler = r =>
                            Console.WriteLine(!r.Error.IsError
                                ? $"Delivered message to {r.TopicPartitionOffset}"
                                : $"Delivery Error: {r.Error.Reason}");

                        _producer = new ProducerBuilder<Null, imageResponse>(config).SetValueSerializer(new AvroSerializer<imageResponse>(schemaRegistry)).Build();

                        // start the main loop and wait for messages to process
                        while (true)
                        {
                            try
                            {
                                var cr = _consumer.Consume(new TimeSpan(0));
                                if (cr != null)
                                {

                                    // store the build request in MongoDB
                                    var document = new BsonDocument();
                                    document.Add("message", $"{cr.Value}");
                                    buildRequestCollection.InsertOne(document);
                                    Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}' from partition: '{cr.Partition}'.");

                                    // build a new image based on the message.
                                    MBImageBuilder newImage = new MBImageBuilder(cr.Value, _producer, bucket);
                                    // start the calc in a new thread.
                                    Parallel.Invoke(() => { newImage.MBCreateImage(); });

                                }
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Error occured: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error occured creating kafka publisher: {e.ToString()}");
                        Environment.Exit(0);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occured creating kafka consumer: {e.ToString()}");
                    Environment.Exit(0);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error occured creating connection to mongodb: {e.ToString()}");
                Environment.Exit(0);
            }
        }
    }
}
