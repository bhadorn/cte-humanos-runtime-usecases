
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

using System.Net.Sockets;
using System.Text;

namespace TCPClient
{
  internal static class TProgram
  {
    #region Constants
    /// <summary>
    /// Constant IP address
    /// </summary>
    private const string Ip = "127.0.0.1";

    /// <summary>
    /// Constant Port number
    /// </summary>
    private const int Port = 7872; 
    #endregion

    /// <summary>
    /// Main methode of the console application
    /// </summary>
    /// <returns></returns>
    public static async Task Main()
    {
      handleKeysToCloseApplication();

      try
      {
        using (TcpClient Client = setupClient())
        {
          await connectToServerAsync(Client).ConfigureAwait(false);

          NetworkStream Stream = Client.GetStream();
          _ = Task.Run(async () =>
          {
            byte[] aui8Buffer = new byte[1024];
            bool bKeepReading = true;

            while (m_bRunning && bKeepReading)
            {
              try
              {
                int iRead = await Stream.ReadAsync(aui8Buffer).ConfigureAwait(false);
                if (iRead == 0)
                {
                  bKeepReading = false;
                }
                else
                {
                  Console.WriteLine(Encoding.UTF8.GetString(aui8Buffer, 0, iRead));
                }
              }
              catch (Exception Exc) when (Exc is not OperationCanceledException)
              {
                bKeepReading = false;
              }
            }
          });

          while (m_bRunning)
          {
            string? nstrInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(nstrInput))
            {
              byte[] aui8Input = Encoding.UTF8.GetBytes(nstrInput);
              await Stream.WriteAsync(aui8Input).ConfigureAwait(false);
            }
          }

          Client.Close();
        }
      }
      catch (Exception Ex)
      {
        Console.WriteLine($"Client error: {Ex.Message}");
      }

      Console.WriteLine("Client exited.");
    }

    /// <summary>
    /// Connects to a the server by Ip and port.
    /// </summary>
    /// <param name="Client">client to connect</param>
    /// <returns></returns>
    private static async Task connectToServerAsync(TcpClient Client)
    {
      await Client.ConnectAsync(Ip, Port).ConfigureAwait(false);
      Console.WriteLine("Connected to server. Press Ctrl+C to exit.");
    }

    /// <summary>
    /// Handles the key pressed event to close the application.
    /// </summary>
    private static void handleKeysToCloseApplication()
    {
      Console.CancelKeyPress += (sender, e) =>
      {
        Console.WriteLine("Closing client...");
        m_bRunning = false;
        Environment.Exit(0);
      };
    }

    /// <summary>
    /// Creates a new client
    /// </summary>
    /// <returns>Created client</returns>
    private static TcpClient setupClient()
    {
      return new TcpClient();
    }

    /// <summary>
    /// Client running flag
    /// </summary>
    private static bool m_bRunning = true;
  }
}
