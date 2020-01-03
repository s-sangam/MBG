// JavaScript source code

db.createUser(
    {
        user: "mongo_user",
        pwd: "password123",
        roles: [
            {
                role: "readWrite",
                db: "MBImageDatabase"
            }
        ]
    }
)
