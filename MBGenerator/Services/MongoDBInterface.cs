using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace MBGenerator.Services
{
    public class MongoDBInterface : IMongoDB
    {

        MongoClient mongoClient;
        IGridFSBucket bucket;

        public  MongoDBInterface()
        {
            try
            {
                MongoCredential credential = MongoCredential.CreateCredential("MBImageDatabase", "mongo_user", "password123");

                var settings = new MongoClientSettings
                {
                    Credential = credential,
                    Server = new MongoServerAddress("mongodb", 27017),
                    UseTls = false,
                };

                mongoClient = new MongoClient(settings);

                bucket = new GridFSBucket(mongoClient.GetDatabase("MBImageDatabase"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error occured opening connectiont to MongoDB: {e.ToString()}");
            }
        }

        public byte[] GetJpegById(String id)
        {
            try
            {
                var imagebinary = bucket.DownloadAsBytes(new ObjectId(id));
                return imagebinary;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to find image with id {id}. Error {e.ToString()}");
                return new byte[0];
            }
        }
    }
}
