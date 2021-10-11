using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using producer.Models;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;


namespace producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TaskController : ControllerBase
    {

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Post([FromBody] producer.Models.Task task)
        {
            var factory = new ConnectionFactory()
            {
                //HostName = "localhost",
                //Port = 31672
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            Console.WriteLine(factory.HostName + ":" + factory.Port);

            var token = GetToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized();
            }

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "TaskQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

               
                var body = Encoding.Default.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(task));              

                channel.BasicPublish(exchange: "",
                                     routingKey: "TaskQueue",
                                     basicProperties: null,
                                     body: body);
            }
            return Ok();
        }

    
        private string GetToken()
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                string uri = "";

                uri ="https://reqres.in/api/login";
              
                client.BaseAddress = new Uri(uri);

                try
                {
                    var responseTask = client.GetAsync("");
                    responseTask.Wait();

                    var result = responseTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        var readTask = result.Content.ReadAsStringAsync();
                        readTask.Wait();

                        var alldata = readTask.Result;
                        var token = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Token>>(alldata);
                        return token.First().token;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught.", e);
                }

            }
            return "";
        }
    }
}
