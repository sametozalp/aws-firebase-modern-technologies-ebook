﻿using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json;
using Telegram.Bot;

namespace EBookTgBot {
    internal class Program {
        static async Task Main(string[] args) {

            string TOKEN = "???????????????????????????????";
            var bot = new TelegramBotClient(TOKEN);
            var chatID = -1002054524437;
            var awsAccessKey = "???????????????????????????????";
            var awsSecretAccessKey = "????????????????????????????";


            string apiUrl = "https://rest.coinapi.io/v1/exchangerate/BTC/USD";
            string APIKEY = "??????????????????????????????????";

            using (HttpClient httpClient = new HttpClient()) {
                httpClient.DefaultRequestHeaders.Add("X-CoinAPI-Key", APIKEY);

                try {
                    string jsonResult = await httpClient.GetStringAsync(apiUrl);

                    JsonDocument jsonDocument = JsonDocument.Parse(jsonResult);
                    var rate = jsonDocument.RootElement.GetProperty("rate");
                    Console.WriteLine(rate);

                    AmazonS3Client s3Client = new AmazonS3Client(
                        awsAccessKey,
                        awsSecretAccessKey,
                        RegionEndpoint.USEast1
                    );

                    var getRequest = new GetObjectRequest {
                        BucketName = "myebook-for-tg",
                        Key = "config.txt"
                    };

                    var savedBTCValue = "";
                    try {
                        using var response = await s3Client.GetObjectAsync(getRequest);

                        using var responseStream = response.ResponseStream;
                        using var reader = new StreamReader(responseStream);

                        savedBTCValue = reader.ReadToEnd();
                        Console.WriteLine(savedBTCValue);

                    } catch (AmazonS3Exception ex) {
                        Console.WriteLine($"Error: {ex.ErrorCode}, Error Message: {ex.Message}");
                    }

                    if (double.Parse(rate.ToString()) != double.Parse(savedBTCValue)) {
                        await bot.SendTextMessageAsync(
                            chatID,
                            "BTC rate changed" +
                            "\nOld BTC Rate: " +  savedBTCValue +
                            "\nNew BTC Rate: " + rate.ToString()
                        );

                        var request = new PutObjectRequest {
                            BucketName = "myebook-for-tg",
                            Key = "config.txt",
                            ContentBody = rate.ToString()
                        };

                        try {
                            await s3Client.PutObjectAsync(request);
                        } catch (AmazonS3Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
                    }
                } catch (HttpRequestException ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}