using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EchoServer
{
    public class Request
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Date { get; set; }
        public string Body { get; set; }
    }

    public class Response
    {
        public string Status { get; set; }
        public string Body { get; set; }
    }
    
    public class Category
    {
        public int Cid { get; set; }
        public string Name { get; set; }
    }
    
    public class Attribut
    {
        public string NameAttribut { get; set; }
        public List<Category> List;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            server.Start();
            
            /* Testing table */
            var categories = new List<Category>
            {
                new Category {Cid = 1, Name = "Beverages"},
                new Category {Cid = 2, Name = "Condiments"},
                new Category {Cid = 3, Name = "Confections"}
            };

            var api = new List<Attribut>
            {
                new Attribut {NameAttribut = "categories", List = categories}
            };

            int i = 4;

            while (true)
            {
                var client = server.AcceptTcpClient();

                var request = client.ReadRequest();
                
                Response response = new Response {};
                
                //////////////////////////////////////////////////////////
                /// 
                /// Testing Constrains
                /// 
                //////////////////////////////////////////////////////////
                
                if ( request.Method == null)
                {
                    response = new Response {Status = response.Status + " missing method,"};
                }
                
                if ( request.Date == null)
                {
                    response = new Response {Status = response.Status + " missing date,"};
                }
                else
                {
                    if (!Microsoft.VisualBasic.Information.IsNumeric(request.Date))
                    {
                        response = new Response {Status = response.Status + " illegal date,"};
                    }
                }
                
                if ( request.Method != "create" && request.Method != "read" && request.Method != "update" && request.Method != "delete" && request.Method != "echo")
                {
                    response = new Response {Status = response.Status + " illegal method,"};
                }
                
                if ( request.Method == "create" || request.Method == "read" || request.Method == "update" || request.Method == "delete")
                {
                    if (request.Path == null)
                    {
                        response = new Response {Status = response.Status + " missing resource,"};
                    }
                }

                if ( request.Method == "create" || request.Method == "update" || request.Method == "echo")
                {
                    if (request.Body == null)
                    {
                        response = new Response {Status = response.Status + " missing body,"};
                    }
                }
                
                if (request.Method == "update")
                {
                    try
                    {
                        request.Body.FromJson<Request>();
                    }
                    catch(Exception e)
                    {
                        response = new Response {Status = response.Status + " illegal body,"};
                    }
                }

                if (request.Method == "echo" && request.Body != null)
                {
                    response = new Response {Body = request.Body};
                }

                if (response.Status != null)
                {
                    response = new Response {Status = "4" + response.Status};
                }
                else if (request.Method != "echo")
                {
                    //////////////////////////////////////////////////////////
                    /// 
                    /// Testing API 
                    /// 
                    //////////////////////////////////////////////////////////

                    var path = request.Path.Split('/');

                    if (path.Length == 4)
                    {
                        try
                        {
                            int.Parse(path[3]);
                            
                            if (request.Method == "read")
                            {
                                try
                                {
                                    Category query =
                                        (from list in (from attribut in api
                                                where attribut.NameAttribut == path[2]
                                                select attribut.List).Single()
                                            where list.Cid == int.Parse(path[3])
                                            select list).Single();
                                    response = new Response {Status = "1 Ok", Body = query.ToJson()};
                                }
                                catch (Exception e)
                                {
                                    response = new Response {Status = "5 not found"};
                                }
                            }

                            if (request.Method == "update")
                            {
                                try{
                                    Category updateValue = request.Body.FromJson<Category>();
                                    
                                    var query =
                                         (from list in (from attribut in api
                                                where attribut.NameAttribut == path[2]
                                                select attribut.List).Single()
                                            where list.Cid == int.Parse(path[3])
                                            select list).Single();
                                    
                                    query.Name = updateValue.Name;
                                    
                                    response = new Response {Status = "3 updated"};
                                }
                                catch (Exception e)
                                {
                                    response = new Response {Status = "5 not found"};
                                }
                            }
                            
                            if (request.Method == "delete")
                            {
                                try{
                                    var query =
                                        (from list in (from attribut in api
                                                where attribut.NameAttribut == path[2]
                                                select attribut.List).Single()
                                            where list.Cid == int.Parse(path[3])
                                            select list).Single();
                                    
                                    categories.Remove(query);
                                    
                                    response = new Response {Status = "1 ok"};
                                }
                                catch (Exception e)
                                {
                                    response = new Response {Status = "5 not found"};
                                }
                            }

                            if (request.Method == "create")
                            {
                                response = new Response {Status = "4 Bad Request"};
                            }
                        }
                        catch (Exception e)
                        {
                            response = new Response {Status = "4 Bad Request"};
                        }
                    }
                    else
                    {
                        if (request.Method == "update" || request.Method == "delete")
                        {
                            response = new Response {Status = "4 Bad Request"};
                        }
                        
                        if (request.Method == "read")
                        {
                            try
                            {
                                Object queryG = (from attribut in api
                                    where attribut.NameAttribut == path[2]
                                    select attribut.List).Single();
                                response = new Response {Status = "1 Ok", Body = queryG.ToJson()};
                            }
                            catch (Exception e)
                            {
                                response = new Response {Status = "4 Bad Request"};
                            }
                        }

                        if (request.Method == "create")
                        {
                            Category newValue = request.Body.FromJson<Category>();
                            
                            var query =
                                (from attribut in api
                                        where attribut.NameAttribut == path[2]
                                        select attribut.List).Single();
                            
                            query.Add(new Category{Cid = i,Name = newValue.Name});
                            
                            var queryG =
                                (from list in (from attribut in api
                                        where attribut.NameAttribut == path[2]
                                        select attribut.List).Single()
                                    where list.Cid == i
                                    select list).Single();

                            i++;
                            Console.WriteLine(queryG.ToJson());
                            response = new Response {Body = queryG.ToJson()};
                        }
                    }
                    
                    
                    
                    /*

                    if (request.Method == "create")
                    {
                        if (path.Length > 3)
                        {
                            response = new Response {Status = "5 not found"};
                        }
                    }
                    
                    if (request.Method == "update")
                    {
                        if (path.Length != 4)
                        {
                            response = new Response {Status = "5 not found"};
                        }
                    }
                    
                    if (request.Method == "delete")
                    {
                        if (path.Length != 4)
                        {
                            response = new Response {Status = "5 not found"};
                        }
                    }*/
                }
                    
                client.SendResponse(response.ToJson());
            }

            server.Stop();
        }
    }
    
    public static class Util
    {
        public static string ToJson(this object data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        public static T FromJson<T>(this string element)
        {
            return JsonSerializer.Deserialize<T>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        public static void SendResponse(this TcpClient client, string response)
        {
            var strm = client.GetStream();
            var msg = Encoding.UTF8.GetBytes(response);
            client.GetStream().Write(msg, 0, msg.Length);
            strm.Close();
        }

        public static Request ReadRequest(this TcpClient client)
        {
            var strm = client.GetStream();
            //strm.ReadTimeout = 250;
            byte[] resp = new byte[2048];
            using (var memStream = new MemoryStream())
            {
                int bytesread = 0;
                do
                {
                    bytesread = strm.Read(resp, 0, resp.Length);
                    memStream.Write(resp, 0, bytesread);

                } while (bytesread == 2048);
                
                var responseData = Encoding.UTF8.GetString(memStream.ToArray());
                return JsonSerializer.Deserialize<Request>(responseData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            }
        }
    }
}
