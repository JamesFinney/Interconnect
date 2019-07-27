using System;
using Teramine.Interconnect;
using Teramine.Interconnect.Models;
using Teramine.Interconnect.Interfaces;
using Teramine.Interconnect.Codecs;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Teramine.Interconnect.Example
{
    /* A simple example program which demonstrates the setting up and use of client/server communications.
     * After setting up the connection and session the client sends a messages every second. The server rejects
     * every 5th messages it receives (this includes the resend).
     * 
     * The example demonstrates the following specific bits:
     * 
     * Server
     * - Creating a connection listener
     * - Accepting a connection and session 
     * - Receiving messages from a session and then accepting and rejected those messages (with response data)
     * 
     * Client
     * - Creating an outgoing connection to the server
     * - Creating a new Session
     * - Sending messages to the server
     * - Responding to Accepted and Rejected messages
     * - Closing the connection
     * 
     * Notes:
     * - As well as rejecting messages the server can also reject sessions
     * - A connection is bidiretional meaning that the server could also create a session with the client
     * 
     * */
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Termamine.Interconnect Example");

            Console.WriteLine("Press any key to stop...");

            // setup and start the listener
            var listener = new Listener("127.0.0.1", 5000, SslSettings.NoSsl);
            listener.OnConnection += Listener_OnConnection;
            listener.Start();

            // create some data to send to the server on connect
            var connectionData = new Dictionary<string, string>
            {
                { "connectionName", "myNewConnection" }
            };

            // create the client connection
            var clientConnection = new Connection("127.0.0.1", 5000, SslSettings.NoSsl, connectionData);

            // create a new session over which message are sent
            var session = clientConnection.CreateSession("myNewSession", Encoding.UTF8.GetBytes("myNewSession"));

            bool stop = false;

            // create the client task which sends a message every second
            var task = Task.Run(() =>
            {
                long number = 0;

                while(!stop)
                {
                    var response = session.Send(Encoding.UTF8.GetBytes($"Please accept message number {number}"));
                    var responseMessage = Encoding.UTF8.GetString(response.Data);

                    // the server has accepted our message
                    if (response.Accepted)
                    {
                        Console.WriteLine($"The server accepted our message {number} with the response '{responseMessage}'");
                    }
                    else
                    {
                        // the server rejected our message
                        Console.WriteLine($"The server rejected our message number {number} with the response '{responseMessage}'. Resending...");
                        response = session.Send(Encoding.UTF8.GetBytes($"Please accept message number {number}"));
                        if(response.Accepted == true)
                        {
                            Console.WriteLine($"The server has now accepted our message number {number}");
                        }
                    }

                    // wair for a second before sending another message
                    Thread.Sleep(1000);
                    number++;
                }
            });

            // wairt for a key press
            Console.ReadKey();

            // flag to stop and wait for the task to end
            stop = true;
            task.Wait();

            // if running in visual studio you may get an exception in InterconnectNetworkStream - nothing to worry about as it's caught elsewhere
            clientConnection.Close();
        }

        // Event hanlder for when a new connection has been created by the client
        private static void Listener_OnConnection(object sender, IConnection connection)
        {
            // listen for incomding sessions
            connection.OnSession += Connection_OnSession;
            connection.OnClosed += (s, e) =>
            {
                Console.WriteLine("Connection closed");
            }; 

            Console.WriteLine("Server received new connection: connectionName [{0}]", connection.Data["connectionName"]);
        }

        // Event handler for incoming sessions from the client
        private static void Connection_OnSession(object sender, Session session)
        {
            Console.WriteLine("Server received new session: Name [{0}]", session.Name);

            session.OnMessage += Session_OnMessage;

            // the receiver of the new session can also reject the session
            session.Accept();
        }

        static long receivedMessages = 0;

        // event handler for when a new message is received on the session
        private static void Session_OnMessage(object sender, Message message)
        {
            receivedMessages++;
            var messageContent = Encoding.UTF8.GetString(message.Data);

            // reject every 5th message received
            if(receivedMessages % 5 == 0)
            {
                message.Reject(Encoding.UTF8.GetBytes($"I'm sorry - I cannot accept your message right now"));
            }
            else
            { 
                message.Accept(Encoding.UTF8.GetBytes($"I accept your message [{messageContent}]"));
            }
        }
    }
}
