
"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/ImageMarshallingHub").build();

connection.start().then(function () {
    console.log("connected to signalr hub");
});

connection.on("RecieveURL", (imageURL, x, y) => {
    var GetJpegURL = "/CloudHome/GetJpegById?Id=";
    var imageURL = GetJpegURL.concat(imageURL);
    var image_id = "image:";
    image_id = image_id.concat(x, ",", y);
    document.getElementById(image_id).src = imageURL;
});
