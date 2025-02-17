﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace console_server_test
{
	class server
	{
		TcpListener m_server_socket = new( IPAddress.Any, 1234 );
		TcpClient m_inspection_client = new();
		TcpClient m_classifying_client = new();

		List<Socket> client_list = new();

		Thread accept_thread = null;
		Thread recv_thread = null;

		public void server_start()
		{
			m_server_socket.Start();

			accept_thread = new( new ThreadStart( fn_accept_thread ) );
			accept_thread.Start();

			recv_thread = new( new ThreadStart( fn_recv_thread ) );
			recv_thread.Start();
		}

		void fn_accept_thread()
		{
			while( true )
			{
				Thread.Sleep( 1 );

				TcpClient tc = m_server_socket.AcceptTcpClient();

				byte[] buff = new byte[ 256 ];
				try
				{
					tc.Client.Receive( buff );
				}
				catch( Exception ex )
				{
					tc.Close();
					continue;
				}
				
				string recv_str = Encoding.Default.GetString( buff );
				recv_str = recv_str.Trim('\0');

				if( recv_str.Contains( "CLIENT_TYPE" ) )
				{
					string[] token = recv_str.Split( '=' );
					if( token.Length < 2 )
					{
						tc.Close();
						continue;
					}

					if( token[ 1 ].ToUpper() == "INSPECTION" )
					{
						m_inspection_client = tc;
						send_inspection_client( "ack" );
					}
					else if( token[ 1 ].ToUpper() == "CLASSIFYING" )
					{
						m_classifying_client = tc;
					}
				}
			}
		}

		string total_data;
		string remaind_data;
		public string image_data;
		void fn_recv_thread()
		{
			total_data = "";
			remaind_data = "";
			while( true )
			{
				Thread.Sleep( 1 );

				if( inspection_client_connected() )
				{
					byte[] buff = new byte[ 4096 ];
					try
					{
						m_inspection_client.Client.Receive( buff );
					}
					catch( Exception ex )
					{
						m_inspection_client.Close();
						continue;
					}
					
					string recv_str = Encoding.Default.GetString( buff );
					recv_str = recv_str.Trim( '\0' );                               //이까지 확인 

					if( remaind_data.Length > 0 )
					{
						recv_str = remaind_data + recv_str;
						remaind_data = "";
					}

					int start_index = recv_str.IndexOf( '<' );
					if( 0 <= start_index )
					{
						total_data = "";
						image_data = "";
						recv_str = recv_str.Remove( 0, start_index );
					}
					total_data += recv_str;

					if( total_data.Contains( '>' ) )
					{
						//int end_index = total_data.IndexOf( '>' );
						string[] split_data = total_data.Split( '>' );
						total_data = split_data[ 0 ];
						remaind_data = split_data[ 1 ];

						total_data = total_data.Remove( 0, 1 );

						if( image_data.Length <= 0 )
						{
							image_data = total_data.Split( '|' )[ 1 ];
						}
					}
					Console.WriteLine( total_data );
				}
			}
		}

		public bool inspection_client_connected()
		{
			if( m_inspection_client.Client != null )
			{

				return m_inspection_client.Client.Connected;
			}

			return false;
		}

		public void send_inspection_client( string _cmd )
		{
			m_inspection_client.Client.Send( Encoding.UTF8.GetBytes( _cmd ) );
		}
	}
}
