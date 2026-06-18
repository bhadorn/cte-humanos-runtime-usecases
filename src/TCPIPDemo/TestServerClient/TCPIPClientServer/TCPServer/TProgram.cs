
/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2022 – www.cybertech.swiss         *
 *****************************************************************************
 * Project: TCP IP Test Client
 * Date   : 16.06.2025
 *****************************************************************************
 * License:                                                                  *
 *   This library is protected software; you are not allowed to redistribute *
 *   whole or part of it to other companies or external persons without the  *
 *   authorization of the CEO CyberTech Engineering GmbH.                    *
 *****************************************************************************/

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TCPServer
{
  internal class TProgram
  {
    #region Constants
    /// <summary>
    /// Constant port number
    /// </summary>
    private const int Port = 7872;
    #endregion

    /// <summary>
    ///  Main methode of the console application
    /// </summary>
    /// <param name="astrArgs"></param>
    /// <returns></returns>
    public static async Task Main(string[] astrArgs)
    {
      TcpListener? nListener = null;

      handleKeysToShutdown(nListener);

      // Server
      nListener = setupServer();
      startServer(nListener);
      // Start server input task
      _ = Task.Run(serverInputLoopAsync);
      await listenToClientsAsync(nListener).ConfigureAwait(false);

      Console.WriteLine("Server stopped.");
    }

    /// <summary>
    /// Broadcasts a message to all connnected clients.
    /// </summary>
    /// <param name="strMessage">message to broadcast</param>
    /// <param name="nSender">the sender of the message. As default the server is the sender.</param>
    /// <returns></returns>
    private static async Task broadcastMessageAsync(string strMessage, TcpClient? nSender = null)
    {
      byte[] aui8Message = Encoding.UTF8.GetBytes(strMessage);

      ///////////////////////////////////////////////////////////
      lock (m_ClientsLock)
      {
        foreach (TcpClient Client in m_lstClients.ToList())
        {
          if (Client.Connected)
          {
            try
            {
              if (nSender == null || Client != nSender)
              {
                NetworkStream Stream = Client.GetStream();
                Stream?.WriteAsync(aui8Message);
              }
            }
            catch (Exception Ex)
            {
              Console.WriteLine($"Broadcast error: {Ex.Message}");
            }
          }
        }
      }
      ///////////////////////////////////////////////////////////
    }

    /// <summary>
    /// Close connection to all connected clients.
    /// </summary>
    private static void cleanUp()
    {
      ///////////////////////////////////////////////////////////
      lock (m_ClientsLock)
      {
        foreach (TcpClient Client in m_lstClients)
        {
          Client.Close();
        }
      }
      ///////////////////////////////////////////////////////////
    }

    /// <summary>
    /// Construct the Inputs as a formatted CSV
    /// </summary>
    /// <param name="strHeader">additional header of the CSV</param>
    /// <returns>formatted CSV with header and record</returns>
    private static string? constructInput(string strHeader)
    {
      // Example:
      //"Name"; "State"; "ErrCode"; "ProgramName"; "StartDateTime"; "ProductionTime"; "OperationTimer"; "PartCounter";
      //"Maschine 97590"; 1; 1086; ""; "--.--.---- --:--:--"; "--:--:--"; "0:22"; 2;
      //"Maschine 97590"; 4; 0; "7029204_short (V 005)"; "22.06.2025 12:19:13"; "00:00:01"; "0:23"; 2;

      Random Random = new Random();

      StringBuilder Sb = new StringBuilder();

      const char cDelimitter = ';';
      string strMachineId = $"\"Maschine {Random.Next(1000, 5000)}\"";
      int iMachineState = Random.Next(1, 5);
      int iErrorId = m_iPartCounter % 3 == 0 ? 0 : Random.Next(1000, 3999);
      string strProgramName = m_iPartCounter % 6 == 0 ? string.Empty : $"\"PRG {Random.Next(1000, 2000)}\"";
      string strStartProductionDateTime = DateTime.Now.ToString();
      string strCurrentCycleTime = "1:10:30";
      string strOperationTime = "11:35";

      Sb.Append(strMachineId).Append(cDelimitter)
        .Append(iMachineState).Append(cDelimitter)
        .Append(iErrorId).Append(cDelimitter)
        .Append(strProgramName).Append(cDelimitter)
        .Append(strStartProductionDateTime).Append(cDelimitter)
        .Append(strCurrentCycleTime).Append(cDelimitter)
        .Append(strOperationTime).Append(cDelimitter)
        .Append(m_iPartCounter).Append(cDelimitter);

      string strCsvRecord = Sb.ToString();

      string strCsvReturn = $"{strHeader}{strCsvRecord}";

      Console.WriteLine($"Sent CSV:\n{strCsvReturn}");

      return strCsvReturn;
    }

    /// <summary>
    /// Separate Task to handle connected clients.
    /// </summary>
    /// <param name="Client">client to handle</param>
    /// <returns></returns>
    private static async Task handleClientAsync(TcpClient Client)
    {
      string? strRemoteEndPoint = Client.Client.RemoteEndPoint?.ToString();
      Console.WriteLine($"Client connected from: {strRemoteEndPoint}.");
      byte[]? aui8Buffer = new byte[1024];
      NetworkStream Stream = Client.GetStream();

      try
      {
        while (Client.Connected && m_bRunning)
        {
          int iRead = await Stream.ReadAsync(aui8Buffer).ConfigureAwait(false);
          if (iRead == 0) break;

          string strMessage = Encoding.UTF8.GetString(aui8Buffer, 0, iRead);
          Console.WriteLine($"Received from {strRemoteEndPoint}: {strMessage}");

          // Echo back
          byte[]? aui8Response = Encoding.UTF8.GetBytes($"Server: {strMessage}");

          await Stream.WriteAsync(aui8Response).ConfigureAwait(false);
        }
      }
      catch (Exception Ex)
      {
        Console.WriteLine($"Client error: {Ex.Message}");
      }
      finally
      {
        Console.WriteLine("Client disconnected.");
        Client.Close();

        ///////////////////////////////////////////////////////////
        lock (m_ClientsLock)
        {
          m_lstClients.Remove(Client);
        }
        ///////////////////////////////////////////////////////////
      }
    }

    /// <summary>
    /// Handles the key pressed event to close the application.
    /// </summary>
    /// <param name="nListener"></param>
    private static void handleKeysToShutdown(TcpListener? nListener)
    {
      Console.CancelKeyPress += (sender, e) =>
      {
        if (nListener != null)
        {
          shutDownServer(nListener);
        }
        Environment.Exit(0);
      };
    }

    /// <summary>
    /// Seperate Task to listen if clients connect to server and add them to connected clients.
    /// </summary>
    /// <param name="Listener"></param>
    /// <returns></returns>
    private static async Task listenToClientsAsync(TcpListener Listener)
    {
      while (m_bRunning)
      {
        try
        {
          TcpClient TcpClient = await Listener.AcceptTcpClientAsync().ConfigureAwait(false);

          if (TcpClient != null)
          {
            /////////////////////////////////////////////
            lock (m_ClientsLock)
            {
              m_lstClients.Add(TcpClient);
            }
            /////////////////////////////////////////////

            _ = handleClientAsync(TcpClient);
          }
        }
        catch (Exception Ex)
        {
          if (m_bRunning)
          {
            Console.WriteLine($"Accept error: {Ex.Message}");
          }
        }
      }
    }

    /// <summary>
    /// Seperated Task to run Server input asynchroniously
    /// </summary>
    /// <returns></returns>
    private static async Task serverInputLoopAsync()
    {
      int iCounter = 0;
      while (m_bRunning)
      {
        string strHeader = string.Empty;
        if (iCounter == 0)
        {
          strHeader = "\"Name\"; \"State\"; \"ErrCode\"; \"ProgramName\"; \"StartDateTime\"; \"ProductionTime\"; \"OperationTimer\"; \"PartCounter\";\n";
        }

        string? strInput = constructInput(strHeader);
        if (!string.IsNullOrWhiteSpace(strInput))
        {
          await broadcastMessageAsync(strInput).ConfigureAwait(false);
        }

        int iTimeout = 5000;
        await Task.Delay(iTimeout).ConfigureAwait(false);
        iCounter = iCounter < 10 ? iCounter + 1 : 0;
      }
    }

    /// <summary>
    /// Creates the Server as a listener
    /// </summary>
    /// <returns></returns>
    private static TcpListener setupServer()
    {
      return new TcpListener(IPAddress.Any, Port);
    }

    /// <summary>
    /// Shuts the server down.
    /// </summary>
    /// <param name="Listener">server as listener</param>
    private static void shutDownServer(TcpListener Listener)
    {
      Console.WriteLine("Shutting down server...");

      Listener.Stop();
      m_bRunning = false;

      cleanUp();

      Console.WriteLine("Server shut down successfully.");
    }

    /// <summary>
    /// Starts the server
    /// </summary>
    /// <param name="nListener">server as listener to start</param>
    private static void startServer(TcpListener? nListener)
    {
      nListener?.Start();
      m_bRunning = true;
      Console.WriteLine($"Server started on port {Port}. Press Ctrl+C to stop.");
    }

    /// <summary>
    /// client lock
    /// </summary>
    private static readonly object m_ClientsLock = new object();

    /// <summary>
    /// All connected clients
    /// </summary>
    private static readonly List<TcpClient> m_lstClients = new List<TcpClient>();

    /// <summary>
    /// Server running flag 
    /// </summary>
    private static bool m_bRunning;

    /// <summary>
    /// Total part counter
    /// </summary>
    private static int m_iPartCounter = 0;
  }
}
