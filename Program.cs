using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace L1
{
    class Program
    {
        static string displayName = "";
        static int localPort; // порт приема сообщений
        static int remotePort; // порт для отправки сообщений
        static Socket listeningSocket;
        static MongoClient client;
        static IMongoCollection<Person> collection;
        static void Main(string[] args)
        {
            StartApp();
        }

        static async void StartApp()
        {
            try
            {
                var intro = "Для отправки сообщений введите сообщение и нажмите Enter";

                client = new MongoClient("mongodb+srv://Dimas:123456987@cluster0.qld3l.mongodb.net/myFirstDatabase?retryWrites=true&w=majority");

                var database = client.GetDatabase("Messanger");
                //database.CreateCollection("People");
                collection = database.GetCollection<Person>("People");

                Console.Write("Введите порт для приема сообщений: ");
                localPort = Int32.Parse(Console.ReadLine());
                Console.Write("Введите порт для отправки сообщений: ");
                remotePort = Int32.Parse(Console.ReadLine());

                var currEndPoint = $"127.0.0.1.{localPort}";

                var person = collection.Find(x => x.endPoint == currEndPoint).ToList();

                if (person.Count == 0)
                {
                    Console.WriteLine("Введите имя под которым вы будете общаться");
                    displayName = Console.ReadLine();

                    var newPerson = new Person
                    {
                        Name = displayName,
                        endPoint = $"127.0.0.1.{localPort}",
                        Messages = new List<string>(),
                    };

                    collection.InsertOne(newPerson);
                }
                else
                {
                    displayName = person[0].Name;
                    intro = "";

                    foreach (var message in person[0].Messages)
                    {
                        Console.WriteLine(message);
                    }
                }

                Console.Write(intro);
                Console.WriteLine();

                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Task listeningTask = new Task(Listen);
                listeningTask.Start();

                // отправка сообщений на разные порты
                while (true)
                {
                    var sender = displayName + ": ";
                    string message = Console.ReadLine();

                    byte[] data = Encoding.Unicode.GetBytes("\t\t" + sender + message);
                    EndPoint remotePoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), remotePort);

                    listeningSocket.SendTo(data, remotePoint);


                    //Cохранение сообщений

                    var curPerson = collection.Find(x => x.endPoint == $"127.0.0.1.{localPort}").ToList()[0];
                    curPerson.Messages.Add(sender + message);

                    collection.ReplaceOne(x => x.endPoint == $"127.0.0.1.{localPort}", curPerson);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close();
            }
        }

        // поток для приема подключений
        private static void Listen()
        {
            try
            {
                //Прослушиваем по адресу
                IPEndPoint localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), localPort);
                listeningSocket.Bind(localIP);

                while (true)
                {
                    // получаем сообщение
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0; // количество полученных байтов
                    byte[] data = new byte[256]; // буфер для получаемых данных

                    //адрес, с которого пришли данные
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

                    do
                    {
                        bytes = listeningSocket.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (listeningSocket.Available > 0);
                    // получаем данные о подключении
                    IPEndPoint remoteFullIp = remoteIp as IPEndPoint;

                    // выводим сообщение
                    Console.WriteLine("{0}", builder.ToString());

                    var curPerson = collection.Find(x => x.endPoint == $"127.0.0.1.{localPort}").ToList()[0];
                    curPerson.Messages.Add(builder.ToString());

                    collection.ReplaceOne(x => x.endPoint == $"127.0.0.1.{localPort}", curPerson);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close();
            }
        }
        // закрытие сокета
        private static void Close()
        {
            if (listeningSocket != null)
            {
                listeningSocket.Shutdown(SocketShutdown.Both);
                listeningSocket.Close();
                listeningSocket = null;
            }
        }
    }
}
