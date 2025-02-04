﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyRabbitProducer.Models;
using RabbitMQ.Client;
using Newtonsoft.Json;
using System.Net.Http;

namespace MyRabbitProducer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TaskController : ControllerBase
    {

        public TaskController()
        {
        }

        // GET TaskController:
        [HttpPost]
        public ActionResult<Token> PostSecureTask(Models.Task taskModel)
        {
            Token objResponse = null;
            try
            {
                objResponse = GetSecureToken(taskModel);

                if (objResponse == null) return StatusCode(401, "Authentication Failed");
                if (string.IsNullOrEmpty(objResponse.token)) return StatusCode(401, "Authentication Failed");

                var factory = new ConnectionFactory()
                {
                    //when run in Visual studio, uncomment this 2 lines
                    //when run in Kubernetes, comment these 2 lines
                    //HostName = "localhost",
                    //Port = 31672

                    //when run in Visual studio, comment this 2 lines
                    //when run in Kubernetes, uncomment these 2 lines, read hostname and port from producer.yaml
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                    Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
                };

                Console.WriteLine(factory.HostName + ":" + factory.Port);
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: "TaskQueue",
                                            type: ExchangeType.Direct,
                                            durable: true,
                                            autoDelete: false);

                    channel.QueueDeclare(queue: "TaskQueue",
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var body = Encoding.UTF8.GetBytes(objResponse.token);

                    channel.BasicPublish(exchange: "TaskQueue",
                                         routingKey: "TaskQueue",
                                         basicProperties: null,
                                         body: body);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return objResponse;
        }

        // token validation
        private Token GetSecureToken(Models.Task objToken)
        {
            Token tokenResponse = null;

            string surl = "https://reqres.in/api/login/";
            using (var client = new System.Net.Http.HttpClient())
            {
                client.BaseAddress = new Uri(surl);

                //HTTP GET
                try
                {
                    var content = new StringContent(JsonConvert.SerializeObject(objToken), System.Text.Encoding.UTF8, "application/json");

                    var responseTask = client.PostAsync(surl, content);
                    responseTask.Wait();

                    var result = responseTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        var readTask = result.Content.ReadAsStringAsync();
                        readTask.Wait();

                        var alldata = readTask.Result;
                        tokenResponse = JsonConvert.DeserializeObject<Token>(alldata);
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            return tokenResponse;
        }
    }
}

