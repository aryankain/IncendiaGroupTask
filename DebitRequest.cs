using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using IncendiaGroupTask.Extension;
using IncendiaGroupTask.Model;

namespace IncendiaGroupTask
{
    public static class DebitRequest
    {
        [FunctionName("DebitRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            DebitRequet request = JsonConvert.DeserializeObject<DebitRequet>(requestBody);

            AccountInfo account = null;
            string responseMessage = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = @"Select * from AccountBalanceDetails Where Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Id", request.Account);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        account = new AccountInfo()
                        {
                            Id = (int)reader["Id"],
                            Balance = (decimal)reader["Balance"],
                            LastUpdatedDate = (DateTime)reader["LastUpdatedDate"]
                        };
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            if (account == null)
            {
                return new NotFoundResult();
            }
            else
            {
                if (account.Balance >= request.Amount)
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                        {
                            connection.Open();
                            var query = $"INSERT INTO [dbo].[TransactionsDetails]([Id],[Amount],[Direction],[Account]) VALUES('{request.Id}', '{request.Amount}','{request.Direction}','{request.Account}');";
                            query += $"UPDATE [dbo].[AccountBalanceDetails] SET [Balance] = [Balance]-'{request.Amount}' WHERE [Id]='{request.Account}';";
                            SqlCommand command = new SqlCommand(query, connection);
                            command.ExecuteNonQuery();

                            responseMessage = $"Amount '{request.Amount}' Debited from '{request.Account}'.";
                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError(e.ToString());
                        return new BadRequestResult();
                    }
                }
                else
                {
                    responseMessage = $"Insufficient balance in account '{request.Account}'.";
                }
            }

            if(string.IsNullOrEmpty(responseMessage))
            {
                return new BadRequestResult();
            }
           

            return new OkObjectResult(responseMessage);
        }


    }






}
