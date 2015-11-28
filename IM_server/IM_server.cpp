#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <stdlib.h>
#include <signal.h>
#include <string.h>
#include <time.h>
#include <stdint.h>
#include "im_package.h"
#include "conn_item.h"
#include "auth_item.h"
#include "compiler_option.h"

#ifdef WINDOWS
#include <Winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
#pragma warning(disable:4996)
#else
#include <errno.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <sys/stat.h>
#include <fcntl.h>
#endif

#ifdef DAEMON
#include "my_daemon.h"
#endif

#ifndef CAPACITY
#define CAPACITY 1048576
#endif

using namespace std;

//constant
const int login_timeout = 5;
const timeval select_timeout = { 10, 0 };	//heartbeat
const bool record_log = true;
const bool record_log_file_transmission = false;

#ifdef DISPLAY
const bool console = true;
#else
const bool console = false;
#endif

#ifdef PIPE
const char * pipe_I2D = "pipe_I2D";
const char * pipe_I2D_r = "pipe_I2D_r";
const char * pipe_D2I = "pipe_D2I";
const char * pipe_D2I_r = "pipe_D2I_r";
const bool pipe_warning = true;
#endif

//log file
FILE * fd_log;
//user file
FILE * fd_user;
//table of listening-socket, named G1
vector<int> lis_sock_table;
//table of connected client, include socket and address, named G2
vector<conn_item> conn_table;
//table of client with authentication, include socket and address, named G3
vector<auth_item> auth_table;
//time
time_t curr_time;
time_t last_conn_time;
//log buffer
char log_buffer[CAPACITY];

#ifdef PIPE
int fd_pipe_I2D;
int fd_pipe_I2D_r;
int fd_pipe_D2I;
int fd_pipe_D2I_r;
char pipe_buf[65536];
uint32_t * pipe_buf_ptr = (uint32_t *)pipe_buf;
uint32_t * pipe_buf_ptr_table = (uint32_t *)(pipe_buf + 12);
int pipe_status;
#endif

//write log: application message
void write_log(char * application_message, char * error_string);
//write log: client table
void write_log(vector<auth_item> * auth_table, int type);
//write log: package
void write_log(uint32_t username, sockaddr_in * endpoint, int communication, bool heartbeat, im_package * package);
//process terminate
void terminate(int no);

#ifdef PIPE
void write_log(uint32_t * pipe_buffer_uint32_t);
int read_confirm(int fd, char * buffer, int size);
int write_confirm(int fd, char * buffer, int size);
#endif

#ifdef WINDOWS
//receive package
int receive_package(SOCKET sock, im_package * pkg, bool peek);
//make sure to send a number of Bit
int send_confirm(SOCKET sock, char * buffer, int size, int flags);
#else
//receive package
int receive_package(int sock, im_package * pkg, bool peek);
//make sure to send a number of Bit
int send_confirm(int sock, char * buffer, int size, int flags);
#endif

int main(int argc, char* argv[])
{
	//operator
	int sock = 0;
	conn_item conn;
	auth_item auth;
	memset(&conn, 0, sizeof(conn));
	memset(&auth, 0, sizeof(auth));
	//returned value
	int rtn_val;
	//port number
	int port_num;
	//socket criteria
	struct addrinfo addr_criteria;
	memset(&addr_criteria, 0, sizeof(addr_criteria));
	addr_criteria.ai_family = AF_INET;
	addr_criteria.ai_socktype = SOCK_STREAM;
	addr_criteria.ai_protocol = IPPROTO_TCP;
	//heartbeat buffer
	char heartbeat_buffer[4];
	//socket set
	fd_set sock_set;
	//maximum descriptor
	int max_descriptor = -1;
	//packege
	im_package pkg;
	memset(&pkg, 0, sizeof(pkg));
	//wait for a few seconds before disconnecting (make sure the last package has been send out)
	int wait_sec = 1;
	//tag
	bool check = false;
	bool update_IM = false;
	bool update_doudizhu = false;
	//temp
	int temp_int = 0;
	uint32_t temp_uint32_t = 0;
	uint32_t * temp_ptr_uint32_t = NULL;
	struct addrinfo * temp_serv;
	char temp_str[32];
	memset(temp_str, 0, 32);
	int temp_size = sizeof(struct sockaddr_in);
	bool temp_bool_1;
	bool temp_bool_2;
	//signal
	signal(SIGINT, terminate);
	signal(SIGTERM, terminate);

#ifdef WINDOWS
	//prepare winsock
	WSAData wsaData;
	if (WSAStartup(MAKEWORD(2, 2), &wsaData))
	{
		return -1;
	}
#endif

#ifdef DAEMON
	//convert to daemon process
	my_daemon(0);
#endif

#ifdef DISPLAY
	fd_log = NULL;
#else
	fd_log = fopen("IM_server.xml", "a");
#endif

	if (record_log)
	{
		//write log
		write_log("Initiate.", "");
	}

	//input
	if (argc < 2)
	{
		if (record_log)
		{
			//write log
			write_log("There must be 1 parameter at least.", "");
		}
		if (record_log)
		{
			//write log
			write_log("Terminate abnormally.", "");
		}
		//terminate abnormally
#ifdef DISPLAY
#ifdef WINDOWS
		system("PAUSE");
#endif
#endif
		return -1;
	}
	for (int i = 1; i <= argc - 1; ++i)
	{
		if (atoi(argv[i]) == 0)
		{
			if (record_log)
			{
				//write log
				sprintf(log_buffer, "The parameter %d must be a non-zero integer.", i);
				write_log(log_buffer, "");
			}
			if (record_log)
			{
				//write log
				write_log("Terminate abnormally.", "");
			}
			//terminate abnormally
#ifdef DISPLAY
#ifdef WINDOWS
			system("PAUSE");
#endif
#endif
			return -1;
		}
	}
	port_num = argc - 1;

#ifdef PIPE
	pipe_status = 1;
	//open pipes
	fd_pipe_I2D = open(pipe_I2D, O_WRONLY);
	if (fd_pipe_I2D == -1)
	{
		if (record_log)
		{
			//write log
			strcpy(log_buffer, "Fail to open ");
			strcat(log_buffer, pipe_I2D);
			strcat(log_buffer, ".");
			write_log(log_buffer, strerror(errno));
		}
		pipe_status = -2;
	}
	fd_pipe_I2D_r = open(pipe_I2D_r, O_WRONLY);
	if (fd_pipe_I2D_r == -1)
	{
		if (record_log)
		{
			//write log
			strcpy(log_buffer, "Fail to open ");
			strcat(log_buffer, pipe_I2D_r);
			strcat(log_buffer, ".");
			write_log(log_buffer, strerror(errno));
		}
		pipe_status = -2;
	}
	fd_pipe_D2I = open(pipe_D2I, O_RDONLY);
	if (fd_pipe_D2I == -1)
	{
		if (record_log)
		{
			//write log
			strcpy(log_buffer, "Fail to open ");
			strcat(log_buffer, pipe_D2I);
			strcat(log_buffer, ".");
			write_log(log_buffer, strerror(errno));
		}
		pipe_status = -2;
	}
	fd_pipe_D2I_r = open(pipe_D2I_r, O_RDONLY);
	if (fd_pipe_D2I_r == -1)
	{
		if (record_log)
		{
			//write log
			strcpy(log_buffer, "Fail to open ");
			strcat(log_buffer, pipe_D2I_r);
			strcat(log_buffer, ".");
			write_log(log_buffer, strerror(errno));
		}
		pipe_status = -2;
	}
	if (pipe_status == 1)
	{
		if (record_log)
		{
			//write log
			write_log("All pipes has been opened.", "");
		}
	}
#endif

	//read user file
	auth_table.clear();
	fd_user = fopen("user.txt", "r");
	if (fd_user == NULL)
	{
		if (record_log)
		{
			//write log
#ifdef WINDOWS
			write_log("Fail to open file \"user.txt\".", "");
#else
			write_log("Fail to open file \"user.txt\".", strerror(errno));
#endif
		}
		if (record_log)
		{
			//write log
			write_log("Terminate abnormally.", "");
		}
		//terminate abnormally
#ifdef DISPLAY
#ifdef WINDOWS
		system("PAUSE");
#endif
#endif
		return -1;
	}
	for (int i = 0; fscanf(fd_user, "%s", temp_str) != EOF; ++i)
	{
		switch (i % 5)
		{
		case 0:
			//begin
			if (strcmp(temp_str, "#") != 0)
			{
				if (record_log)
				{
					//write log
					write_log("File \"user.txt\" has incorrect format.", "");
				}
				if (record_log)
				{
					//write log
					write_log("Terminate abnormally.", "");
				}
				//terminate abnormally
#ifdef DISPLAY
#ifdef WINDOWS
				system("PAUSE");
#endif
#endif
				return -1;
			}
			break;
		case 1:
			//auth.id
			auth.id = uint32_t(atoi(temp_str));
			if (auth.id == 0)
			{
				if (record_log)
				{
					//write log
					write_log("Username in file \"user.txt\" must be a non-zero integer.", "");
				}
				if (record_log)
				{
					//write log
					write_log("Terminate abnormally.", "");
				}
				//terminate abnormally
#ifdef DISPLAY
#ifdef WINDOWS
				system("PAUSE");
#endif
#endif
				return -1;
			}
			break;
		case 2:
			//auth.pwd
			strcpy(auth.pwd, temp_str);
			break;
		case 3:
			//score, need not to read
			break;
		case 4:
			//end
			if (strcmp(temp_str, "#") != 0)
			{
				if (record_log)
				{
					//write log
					write_log("File \"user.txt\" has incorrect format.", "");
				}
				if (record_log)
				{
					//write log
					write_log("Terminate abnormally.", "");
				}
				//terminate abnormally
#ifdef DISPLAY
#ifdef WINDOWS
				system("PAUSE");
#endif
#endif
				return -1;
			}
			//add auth to auth_table
			auth.IM_online = false;
			auth_table.push_back(auth);
			//clear auth
			memset(&auth, 0, sizeof(auth));
			break;
		default:
			break;
		}
	}
	memset(&auth, 0, sizeof(auth));
	fclose(fd_user);
	if (record_log)
	{
		//write log
		write_log("User file \"user.txt\" has been read.", "");
	}
	if (record_log)
	{
		//write log
		write_log(&auth_table, 0);
	}

	//listening socket: get ready
	temp_int = 0;
	for (int i = 1; i <= port_num; ++i)
	{
		rtn_val = getaddrinfo("0.0.0.0", argv[i], &addr_criteria, &temp_serv);
		if (rtn_val != 0)
		{
			continue;
		}
		for (struct addrinfo * addr = temp_serv; addr != NULL; addr = (addr->ai_next))
		{
			sock = socket(addr->ai_family, addr->ai_socktype, addr->ai_protocol);
			if (sock != -1)
			{
				//set socket option
				rtn_val = 1;
#ifdef WINDOWS
				rtn_val = setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, (const char *)& rtn_val, sizeof(rtn_val));
#else
				rtn_val = setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, (void *)& rtn_val, sizeof(rtn_val));
#endif
				if (rtn_val < 0)
				{
					if (record_log)
					{
						//write log
#ifdef WINDOWS
						write_log("Fail to set listening-socket option.", "");
#else
						write_log("Fail to set listening-socket option.", strerror(errno));
#endif
					}
					//Fail to create listening-socket. Try next address.
#ifdef WINDOWS
					closesocket(sock);
#else
					close(sock);
#endif
					sock = -1;
					continue;
				}
				//bind socket
				rtn_val = bind(sock, addr->ai_addr, addr->ai_addrlen);
				if (rtn_val == -1)
				{
					if (record_log)
					{
						//write log
#ifdef WINDOWS
						write_log("Fail to bind listening-socket.", "");
#else
						write_log("Fail to bind listening-socket.", strerror(errno));
#endif
					}
					//Fail to bind listening-socket. Try next address.
#ifdef WINDOWS
					closesocket(sock);
#else
					close(sock);
#endif
					sock = -1;
					continue;
				}
				//listen socket
				rtn_val = listen(sock, 10);
				if (rtn_val == -1)
				{
					if (record_log)
					{
						//write log
#ifdef WINDOWS
						write_log("Fail to listen listening-socket.", "");
#else
						write_log("Fail to listen listening-socket.", strerror(errno));
#endif
					}
					//Fail to listen listening-socket. Try next address.
#ifdef WINDOWS
					closesocket(sock);
#else
					close(sock);
#endif
					sock = -1;
					continue;
				}
				//Only one listening-socket should be prepared on a port.
				break;
			}
			else
			{
				//Fail to create listening-socket. Try next address.
				continue;
			}
		}
		freeaddrinfo(temp_serv);
		if (sock == -1)
		{
			if (record_log)
			{
				//write log
				sprintf(log_buffer, "Listening-socket on port %s is unprepared.", argv[i]);
				write_log(log_buffer, "");
			}
		}
		else
		{
			//add to G1
			lis_sock_table.push_back(sock);
			temp_int += 1;
			if (record_log)
			{
				//write log
				sprintf(log_buffer, "Listening-socket on port %s is ready.", argv[i]);
				write_log(log_buffer, "");
			}
		}
	}
	if (temp_int < port_num)
	{
		if (record_log)
		{
			//write log
			write_log("Not all listening-sockets are ready.", "");
		}
	}
	else if (temp_int == 0)
	{
		if (record_log)
		{
			//write log
			write_log("All listening-sockets are unprepared.", "");
		}
		if (record_log)
		{
			//write log
			write_log("Terminate abnormally.", "");
		}
		//terminate abnormally
#ifdef DISPLAY
#ifdef WINDOWS
		system("PAUSE");
#endif
#endif
		return -1;
	}

	//Working
	conn_table.clear();
	while (true)
	{
		//clear socket set
		FD_ZERO(&sock_set);
#ifdef PIPE
		if (pipe_status == 1)
		{
			//add pipe to socket set
			FD_SET(fd_pipe_D2I_r, &sock_set);
			if (max_descriptor < fd_pipe_D2I_r)
			{
				max_descriptor = fd_pipe_D2I_r;
			}
		}
		else
		{
			if (pkg.type == im_package::tp_file && pkg.information == im_package::fl_content)
			{
				//nothing
			}
			else if (pkg.type == im_package::tp_file && pkg.information == im_package::fl_reply)
			{
				//nothing
			}
			else
			{
				if (pipe_status == -1)
				{
					if (record_log && pipe_warning)
					{
						//write log
						write_log("Warning: pipe_D2I_r has been closed by DDZ_server, communication between IM_server and DDZ_server has been terminated.", "");
					}
				}
				else //pipe_status == -2
				{

					if (record_log && pipe_warning)
					{
						//write log
						write_log("Warning: pipes between IM_server and DDZ_server have not been opened yet.", "");
					}
				}
			}
		}
#endif
		//add socket in G1 to socket set
		for (unsigned int i = 0; i < lis_sock_table.size(); ++i)
		{
			FD_SET(lis_sock_table[i], &sock_set);
			if (max_descriptor < lis_sock_table[i])
			{
				max_descriptor = lis_sock_table[i];
			}
		}
		//add socket in G2 to socket set
		for (unsigned int i = 0; i < conn_table.size(); ++i)
		{
			FD_SET(conn_table[i].sock, &sock_set);
			if (max_descriptor < conn_table[i].sock)
			{
				max_descriptor = conn_table[i].sock;
			}
		}
		//add socket in G3 to socket set
		for (unsigned int i = 0; i < auth_table.size(); ++i)
		{
			if (auth_table[i].IM_online)
			{
				FD_SET(auth_table[i].sock, &sock_set);
				if (max_descriptor < auth_table[i].sock)
				{
					max_descriptor = auth_table[i].sock;
				}
			}
		}
		//select
		rtn_val = select(max_descriptor + 1, &sock_set, NULL, NULL, (timeval *)(&select_timeout));
		if (rtn_val == -1)
		{
			//do nothing
		}
		else if (rtn_val == 0)
		{
			//use heartbeat to check out online client
			for (unsigned int i = 0; i < auth_table.size(); ++i)
			{
				if (auth_table[i].IM_online)
				{
					if (record_log)
					{
						sprintf(log_buffer, "heartbeat, client %u (%s : %u)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
						write_log(log_buffer, "");
					}
					//send heartbeat
					rtn_val = send_confirm(auth_table[i].sock, "\xFF\xFF\xFF\xFF", 4, 0);
					if (rtn_val == -1)
					{
						//close socket
#ifdef WINDOWS
						closesocket(auth_table[i].sock);
#else
						close(auth_table[i].sock);
#endif
						//reset
						auth_table[i].IM_online = false;
						auth_table[i].sock = 0;
						memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
						continue;
					}
					//receive heartbeat
					rtn_val = recv(auth_table[i].sock, heartbeat_buffer, 4, MSG_PEEK);
					if (rtn_val <= 0)
					{
						//close socket
#ifdef WINDOWS
						closesocket(auth_table[i].sock);
#else
						close(auth_table[i].sock);
#endif
						//reset
						auth_table[i].IM_online = false;
						auth_table[i].sock = 0;
						memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
					}
				}
			}
		}
		else //rtn_val > 0
		{
#ifdef PIPE
			//request from pipe
			if (FD_ISSET(fd_pipe_D2I_r, &sock_set))
			{
				//read fd_pipe_D2I_r
				rtn_val = read_confirm(fd_pipe_D2I_r, pipe_buf, 1);
				if (rtn_val < 0)
				{
					if (record_log)
					{
						//write log
						strcpy(log_buffer, "Fail to read ");
						strcat(log_buffer, pipe_D2I_r);
						strcat(log_buffer, ".");
						write_log(log_buffer, strerror(errno));
					}
				}
				else if (rtn_val == 0)
				{
					pipe_status = -1;
				}
				else
				{
					//read fd_pipe_D2I
					rtn_val = read_confirm(fd_pipe_D2I, pipe_buf, 4);
					if (rtn_val <= 0)
					{
						if (record_log)
						{
							//write log
							strcpy(log_buffer, "Fail to read ");
							strcat(log_buffer, pipe_D2I);
							strcat(log_buffer, ".");
							write_log(log_buffer, strerror(errno));
						}
					}
					else
					{
						//continue to read fd_pipe_D2I
						rtn_val = read_confirm(fd_pipe_D2I, pipe_buf + 4, pipe_buf_ptr[0] - 4);
						if (rtn_val <= 0)
						{
							if (record_log)
							{
								//write log
								strcpy(log_buffer, "Fail to read ");
								strcat(log_buffer, pipe_D2I);
								strcat(log_buffer, ".");
								write_log(log_buffer, strerror(errno));
							}
						}
						else
						{
							if (record_log)
							{
								//write log
								write_log(pipe_buf_ptr);
							}
							switch (pipe_buf_ptr[1])
							{
							case 2:
								//doudizhu online client table
								if (record_log)
								{
									//write log
									write_log("Receive data from pipe_D2I: doudizhu online client table.", "");
								}
								for (unsigned int i = 0; i < auth_table.size(); ++i)
								{
									for (unsigned int j = 0; j < pipe_buf_ptr[2]; ++j)
									{
										if (auth_table[i].id == pipe_buf_ptr_table[j])
										{
											auth_table[i].doudizhu_online = true;
											break;
										}
										else
										{
											auth_table[i].doudizhu_online = false;
										}
									}
								}
								//The latest doudizhu on-line client should be updated to all on-line clients.
								update_doudizhu = true;
								break;
							case 3:
								//doudizhu invitation
								if (record_log)
								{
									//write log
									write_log("Receive data from pipe_D2I: doudizhu invitation.", "");
								}
								temp_bool_1 = false;
								for (unsigned int i = 0; i < auth_table.size(); ++i)
								{
									if (auth_table[i].id == pipe_buf_ptr[3])
									{
										temp_bool_1 = true;
										if (auth_table[i].doudizhu_online)
										{
											//prepare
											pipe_buf_ptr[0] = 20;
											pipe_buf_ptr[1] = 4;
											//switch [2] and [3]
											temp_uint32_t = pipe_buf_ptr[2];
											pipe_buf_ptr[2] = pipe_buf_ptr[3];
											pipe_buf_ptr[3] = temp_uint32_t;
											//reply
											pipe_buf_ptr[4] = 1;
											//write fd_pipe_I2D
											rtn_val = write_confirm(fd_pipe_I2D, pipe_buf, pipe_buf_ptr[0]);
											if (rtn_val <= 0)
											{
												if (record_log)
												{
													//write log
													strcpy(log_buffer, "Fail to write ");
													strcat(log_buffer, pipe_I2D);
													strcat(log_buffer, ".");
													write_log(log_buffer, strerror(errno));
												}
											}
											else
											{
												if (record_log)
												{
													//write log
													write_log("Send data to pipe_I2D: doudizhu reply (Client is online in doudizhu server).", "");
												}
												if (record_log)
												{
													//write log
													write_log(pipe_buf_ptr);
												}
											}
											//write fd_pipe_I2D_r
											rtn_val = write_confirm(fd_pipe_I2D_r, pipe_buf, 1);
											if (rtn_val <= 0)
											{
												if (record_log)
												{
													//write log
													strcpy(log_buffer, "Fail to write ");
													strcat(log_buffer, pipe_I2D_r);
													strcat(log_buffer, ".");
													write_log(log_buffer, strerror(errno));
												}
											}
											else
											{
												if (record_log)
												{
													//write log
													write_log("Complete.", "");
												}
											}
										}
										else if (auth_table[i].IM_online == false)
										{
											//prepare
											pipe_buf_ptr[0] = 20;
											pipe_buf_ptr[1] = 4;
											//switch [2] and [3]
											temp_uint32_t = pipe_buf_ptr[2];
											pipe_buf_ptr[2] = pipe_buf_ptr[3];
											pipe_buf_ptr[3] = temp_uint32_t;
											//reply
											pipe_buf_ptr[4] = 0;
											//write fd_pipe_I2D
											rtn_val = write_confirm(fd_pipe_I2D, pipe_buf, pipe_buf_ptr[0]);
											if (rtn_val <= 0)
											{
												if (record_log)
												{
													//write log
													strcpy(log_buffer, "Fail to write ");
													strcat(log_buffer, pipe_I2D);
													strcat(log_buffer, ".");
													write_log(log_buffer, strerror(errno));
												}
											}
											else
											{
												if (record_log)
												{
													//write log
													write_log("Send data to pipe_I2D: doudizhu reply (Client is offline in IM server).", "");
												}
												if (record_log)
												{
													//write log
													write_log(pipe_buf_ptr);
												}
											}
											//write fd_pipe_I2D_r
											rtn_val = write_confirm(fd_pipe_I2D_r, pipe_buf, 1);
											if (rtn_val <= 0)
											{
												if (record_log)
												{
													//write log
													strcpy(log_buffer, "Fail to write ");
													strcat(log_buffer, pipe_I2D_r);
													strcat(log_buffer, ".");
													write_log(log_buffer, strerror(errno));
												}
											}
											else
											{
												if (record_log)
												{
													//write log
													write_log("Complete.", "");
												}
											}
										}
										else
										{
											if (record_log)
											{
												//write log
												write_log("Forward doudizhu invitation from pipe_I2D to client.", "");
											}
											//pack
											memset(&pkg, 0, im_package::size_head);
											//pkg.sender
											pkg.type = im_package::tp_doudizhu;
											pkg.information = im_package::dz_invite;
											//pkg.receiver_number
											pkg.receiver[17] = pipe_buf_ptr[2];
											pkg.receiver[18] = pipe_buf_ptr[3];
											pkg.receiver[19] = pipe_buf_ptr[4];
											pkg.content_lenth = 0;
											pkg.build();
											//send
											rtn_val = send_confirm(auth_table[i].sock, pkg.buffer, im_package::size_head, 0);
											if (rtn_val == -1)
											{
												if (record_log)
												{
													write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
												}
												//close socket
												close(auth_table[i].sock);
												if (record_log)
												{
													//write log
													sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
													write_log(log_buffer, "");
												}
												//reset
												auth_table[i].IM_online = false;
												auth_table[i].sock = 0;
												memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
												//The latest IM on-line client should be updated to all on-line clients.
												update_IM = true;
											}
											else
											{
												if (record_log)
												{
													//write log
													write_log(auth_table[i].id, &(auth_table[i].addr), 2, false, &pkg);
												}
												if (record_log)
												{
													//write log
													write_log("Send to client.", "");
												}
											}
										}
										break;
									}
								}
								if (temp_bool_1 == false)
								{
									//prepare
									pipe_buf_ptr[0] = 20;
									pipe_buf_ptr[1] = 4;
									//switch [2] and [3]
									temp_uint32_t = pipe_buf_ptr[2];
									pipe_buf_ptr[2] = pipe_buf_ptr[3];
									pipe_buf_ptr[3] = temp_uint32_t;
									//reply
									pipe_buf_ptr[4] = 0;
									//write fd_pipe_I2D
									rtn_val = write_confirm(fd_pipe_I2D, pipe_buf, pipe_buf_ptr[0]);
									if (rtn_val <= 0)
									{
										if (record_log)
										{
											//write log
											strcpy(log_buffer, "Fail to write ");
											strcat(log_buffer, pipe_I2D);
											strcat(log_buffer, ".");
											write_log(log_buffer, strerror(errno));
										}
									}
									else
									{
										if (record_log)
										{
											//write log
											write_log("Send data to pipe_I2D: doudizhu reply (Client is offline in IM server).", "");
										}
										if (record_log)
										{
											//write log
											write_log(pipe_buf_ptr);
										}
									}
									//write fd_pipe_I2D_r
									rtn_val = write_confirm(fd_pipe_I2D_r, pipe_buf, 1);
									if (rtn_val <= 0)
									{
										if (record_log)
										{
											//write log
											strcpy(log_buffer, "Fail to write ");
											strcat(log_buffer, pipe_I2D_r);
											strcat(log_buffer, ".");
											write_log(log_buffer, strerror(errno));
										}
									}
									else
									{
										if (record_log)
										{
											//write log
											write_log("Complete.", "");
										}
									}
								}
								break;
							default:
								break;
							}
						}
					}
				}
			}
#endif
			//request from socket in G1
			for (unsigned int i = 0; i < lis_sock_table.size(); ++i)
			{
				if (FD_ISSET(lis_sock_table[i], &sock_set))
				{
					//accept
					conn.sock = accept(lis_sock_table[i], (struct sockaddr *) & conn.addr, (socklen_t *)& temp_size);
					if (conn.sock == -1)
					{
						if (record_log)
						{
							//write log
#ifdef WINDOWS
							write_log("Fail to accept socket.", "");
#else
							write_log("Fail to accept socket.", strerror(errno));
#endif
						}
					}

					temp_int = 1048576;
					//set timeout of function "recv()"
					if (record_log)
					{
						//write log
						sprintf(log_buffer, "Connect to client %s : %u", inet_ntoa(conn.addr.sin_addr), conn.addr.sin_port);
						write_log(log_buffer, "");
					}
					//pack
					memset(&pkg, 0, im_package::size_head);
					pkg.sender = 0;
					pkg.type = im_package::tp_login;
					pkg.information = im_package::lg_ask;
					pkg.build();
					//send
					rtn_val = send_confirm(conn.sock, pkg.buffer, im_package::size_head, 0);
					if (rtn_val == -1)
					{
						if (record_log)
						{
							//write log
#ifdef WINDOWS
							write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
							write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
						}
						//close socket
#ifdef WINDOWS
						closesocket(conn.sock);
#else
						close(conn.sock);
#endif
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Disconnect from client %s : %u", inet_ntoa(conn.addr.sin_addr), conn.addr.sin_port);
							write_log(log_buffer, "");
						}
					}
					else
					{
						if (record_log)
						{
							//write log
							write_log(0, &(conn.addr), 2, false, &pkg);
						}
						//set connected time
						conn.set_tim_val();
						//add
						conn_table.push_back(conn);
					}
					memset(&conn, 0, sizeof(conn));
				}
			}
			//Avoid to select the same socket twice in G2 and G3. Use for judge condition of continuing in cycle "while".
			temp_bool_1 = false;
			//request from socket in G2
			for (unsigned int i = 0; i < conn_table.size(); ++i)
			{
				if (FD_ISSET(conn_table[i].sock, &sock_set))
				{
					rtn_val = receive_package(conn_table[i].sock, &pkg, false);
					if (rtn_val == -1)
					{
						if (record_log)
						{
							//write log
#ifdef WINDOWS
							write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
							if (errno == EWOULDBLOCK)
							{
								write_log("Login time-out.", strerror(errno));
							}
							else
							{
								write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
							}
#endif
						}
						//close socket
#ifdef WINDOWS
						closesocket(conn_table[i].sock);
#else
						close(conn_table[i].sock);
#endif
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Disconnect from client %s : %u", inet_ntoa(conn_table[i].addr.sin_addr), conn_table[i].addr.sin_port);
							write_log(log_buffer, "");
						}
						//remove item in "conn_table"
						conn_table.erase(conn_table.begin() + i);
					}
					else if (rtn_val == 0)
					{
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Socket of client %s : %u is closed.", inet_ntoa(conn_table[i].addr.sin_addr), conn_table[i].addr.sin_port);
							write_log(log_buffer, "");
						}
						//close socket
#ifdef WINDOWS
						closesocket(conn_table[i].sock);
#else
						close(conn_table[i].sock);
#endif
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Disconnect from client %s : %u", inet_ntoa(conn_table[i].addr.sin_addr), conn_table[i].addr.sin_port);
							write_log(log_buffer, "");
						}
						//remove item in "conn_table"
						conn_table.erase(conn_table.begin() + i);
					}
					else
					{
						//package
						//split package
						pkg.split();
						if (record_log)
						{
							//write log
							write_log(0, &(conn_table[i].addr), 1, false, &pkg);
						}
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Receive password package from client %s : %u", inet_ntoa(conn_table[i].addr.sin_addr), conn_table[i].addr.sin_port);
							write_log(log_buffer, "");
						}
						if (pkg.type == im_package::tp_login && pkg.information == im_package::lg_answer)
						{
							for (unsigned int j = 0; j < auth_table.size(); ++j)
							{
								if (auth_table[j].id == pkg.sender)
								{
									if (pkg.content_lenth > 32)
									{
										//Password is too long to identify.
										temp_str[0] = '\0';
									}
									else
									{
										memcpy(temp_str, pkg.content, pkg.content_lenth);
										temp_str[pkg.content_lenth] = '\0';
									}
									if (strcmp(auth_table[j].pwd, temp_str) == 0)
									{
										//succeed
										if (record_log)
										{
											//write log
											write_log("Authentication succeeded.", "");
										}
										if (auth_table[j].IM_online)
										{
											//This ID has been used to log in.
											//disconnect old address
											if (record_log)
											{
												//write log
												sprintf(log_buffer, "Client %u logged in again.", j);
												write_log(log_buffer, "");
											}
											//pack
											memset(&pkg, 0, im_package::size_head);
											//pkg.sender
											pkg.type = im_package::tp_login;
											pkg.information = im_package::lg_logout_force;
											//pkg.receiver_number
											//pkg.receiver
											sprintf(pkg.content, "%s : %u", inet_ntoa(conn_table[i].addr.sin_addr), ntohs(conn_table[i].addr.sin_port));
											pkg.content_lenth = strlen(pkg.content);
											pkg.build();
											//send
											send_confirm(auth_table[j].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
											if (record_log)
											{
												//write log
												write_log(auth_table[j].id, &(auth_table[j].addr), 2, false, &pkg);
											}
#ifdef WINDOWS
											//wait for a period of time to make sure that the last package has been sent out
											Sleep(wait_sec);
											//close socket
											closesocket(auth_table[j].sock);
#else
											//wait for a period of time to make sure that the last package has been sent out
											usleep(wait_sec * 1000);
											//close socket
											close(auth_table[j].sock);
#endif
											if (record_log)
											{
												//write log
												sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[j].id, inet_ntoa(auth_table[j].addr.sin_addr), auth_table[j].addr.sin_port);
												write_log(log_buffer, "");
											}
										}
										//set
										auth_table[j].IM_online = true;
										auth_table[j].set_sock_addr(conn_table[i]);
										//remove item in "conn_table"
										conn_table.erase(conn_table.begin() + i);
										//pack
										memset(&pkg, 0, im_package::size_head);
										//pkg.sender
										pkg.type = im_package::tp_login;
										pkg.information = im_package::lg_accept;
										//pkg.receiver_number
										//pkg.receiver
										//pkg.content_lenth
										pkg.build();
										//send
										rtn_val = send_confirm(auth_table[j].sock, pkg.buffer, im_package::size_head, 0);
										if (rtn_val == -1)
										{

										}
										else
										{
											if (record_log)
											{
												//write log
												write_log(auth_table[j].id, &(auth_table[j].addr), 2, false, &pkg);
											}
											if (record_log)
											{
												//write log
												write_log("Repeat.", "");
											}
											if (record_log)
											{
												//write log
												sprintf(log_buffer, "Connect to client %u (%s : %u)", auth_table[j].id, inet_ntoa(auth_table[j].addr.sin_addr), auth_table[j].addr.sin_port);
												write_log(log_buffer, "");
											}
										}
										//pack
										memset(&pkg, 0, im_package::size_head);
										//pkg.sender
										pkg.type = im_package::tp_table;
										pkg.information = im_package::tb_all;
										//pkg.receiver_number
										//pkg.receiver
										temp_ptr_uint32_t = (uint32_t *)pkg.content;
										int a = auth_table.size();
										for (unsigned int k = 0; k < auth_table.size(); ++k)
										{
											temp_ptr_uint32_t[k] = htonl(auth_table[k].id);
										}
										pkg.content_lenth = (auth_table.size() << 2);
										pkg.build();
										//send
										rtn_val = send_confirm(auth_table[j].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
										if (rtn_val <= 0)
										{

										}
										else
										{
											if (record_log)
											{
												//write log
												write_log(auth_table[j].id, &(auth_table[j].addr), 2, false, &pkg);
											}
											if (record_log)
											{
												//write log
												write_log("Send client table to it.", "");
											}
										}
										//Avoid to select the same socket twice in G2 and G3. Use for judge condition of continuing in cycle "while".
										temp_bool_1 = true;
										//The latest IM on-line client should be updated to all on-line clients.
										update_IM = true;
									}
									else
									{
										//incorrect password
										if (record_log)
										{
											//write log
											write_log("Authentication failed: incorrect password.", "");
										}
										//pack
										memset(&pkg, 0, im_package::size_head);
										//pkg.sender
										pkg.type = im_package::tp_login;
										pkg.information = im_package::lg_reject_password;
										//pkg.receiver_number
										//pkg.receiver
										//pkg.content_lenth
										pkg.build();
										//send
										send_confirm(conn_table[i].sock, pkg.buffer, im_package::size_head, 0);
										if (record_log)
										{
											//write log
											write_log(0, &(conn_table[i].addr), 2, false, &pkg);
										}
										if (record_log)
										{
											//write log
											write_log("Repeat.", "");
										}
#ifdef WINDOWS
										//wait for a period of time to make sure that the last package has been sent out
										Sleep(wait_sec);
										//close socket
										closesocket(conn_table[i].sock);
#else
										//wait for a period of time to make sure that the last package has been sent out
										usleep(wait_sec * 1000);
										//close socket
										close(conn_table[i].sock);
#endif
										if (record_log)
										{
											//write log
											sprintf(log_buffer, "Disconnect from client %s : %u", inet_ntoa(conn_table[i].addr.sin_addr), conn_table[i].addr.sin_port);
											write_log(log_buffer, "");
										}
										//remove
										conn_table.erase(conn_table.begin() + i);
										//Avoid to select the same socket twice in G2 and G3. Use for judge condition of continuing in cycle "while".
										temp_bool_1 = false;
									}
									//client in table or not
									temp_bool_2 = true;
									break;
								}
								else
								{
									//Avoid to select the same socket twice in G2 and G3. Use for judge condition of continuing in cycle "while".
									temp_bool_1 = false;
									//client in table or not
									temp_bool_2 = false;
								}
							}
							if (temp_bool_2 == false)
							{
								//non-existent username
								if (record_log)
								{
									//write log
									write_log("Authentication failed: non-existent username.", "");
								}
								//pack
								memset(&pkg, 0, im_package::size_head);
								//pkg.sender
								pkg.type = im_package::tp_login;
								pkg.information = im_package::lg_reject_username;
								//pkg.receiver_number
								//pkg.receiver
								//pkg.content_lenth
								pkg.build();
								//send
								send_confirm(conn_table[i].sock, pkg.buffer, im_package::size_head, 0);
								if (record_log)
								{
									//write log
									write_log(0, &(conn_table[i].addr), 2, false, &pkg);
								}
								if (record_log)
								{
									//write log
									write_log("Repeat.", "");
								}
#ifdef WINDOWS
								//wait for a period of time to make sure that the last package has been sent out
								Sleep(wait_sec);
								//close socket
								closesocket(conn_table[i].sock);
#else
								//wait for a period of time to make sure that the last package has been sent out
								usleep(wait_sec * 1000);
								//close socket
								close(conn_table[i].sock);
#endif
								if (record_log)
								{
									//write log
									sprintf(log_buffer, "Disconnect from client %s : %u", inet_ntoa(conn_table[i].addr.sin_addr), conn_table[i].addr.sin_port);
									write_log(log_buffer, "");
								}
								//remove
								conn_table.erase(conn_table.begin() + i);
							}
						}
						else
						{
							//incorrect login format
							if (record_log)
							{
								//write log
								write_log("Authentication failed: incorrect format.", "");
							}
							//pack
							memset(&pkg, 0, im_package::size_head);
							//pkg.sender
							pkg.type = im_package::tp_login;
							pkg.information = im_package::lg_reject_format;
							//pkg.receiver_number
							//pkg.receiver
							//pkg.content_lenth
							pkg.build();
							//send
							send_confirm(conn_table[i].sock, pkg.buffer, im_package::size_head, 0);
							if (record_log)
							{
								//write log
								write_log(0, &(conn_table[i].addr), 2, false, &pkg);
							}
							if (record_log)
							{
								//write log
								write_log("Repeat.", "");
							}
#ifdef WINDOWS
							//wait for a period of time to make sure that the last package has been sent out
							Sleep(wait_sec);
							//close socket
							closesocket(conn_table[i].sock);
#else
							//wait for a period of time to make sure that the last package has been sent out
							usleep(wait_sec * 1000);
							//close socket
							close(conn_table[i].sock);
#endif
							if (record_log)
							{
								//write log
								sprintf(log_buffer, "Disconnect from client %s : %u", inet_ntoa(conn_table[i].addr.sin_addr), conn_table[i].addr.sin_port);
								write_log(log_buffer, "");
							}
							//remove
							conn_table.erase(conn_table.begin() + i);
						}
					}
				}
			}
			//Avoid to select the same socket twice in G2 and G3. Use for judge condition of continuing in cycle "while".
			if (temp_bool_1)
			{
				continue;
			}
			//request from socket in G3
			for (unsigned int i = 0; i < auth_table.size(); ++i)
			{
				if (FD_ISSET(auth_table[i].sock, &sock_set))
				{
					rtn_val = receive_package(auth_table[i].sock, &pkg, false);
					if (rtn_val == -1)
					{
						if (record_log)
						{
							//write log
#ifdef WINDOWS
							write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
							write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
						}
						//close socket
#ifdef WINDOWS
						closesocket(auth_table[i].sock);
#else
						close(auth_table[i].sock);
#endif
						//The latest IM on-line client should be updated to all on-line clients.
						update_IM = true;
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Disconnect from client %s : %u", inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
							write_log(log_buffer, "");
						}
						//reset
						auth_table[i].IM_online = false;
						auth_table[i].sock = 0;
						memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
					}
					else if (rtn_val == 0)
					{
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Socket of client %s : %u is closed.", inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
							write_log(log_buffer, "");
						}
						//close socket
#ifdef WINDOWS
						closesocket(auth_table[i].sock);
#else
						close(auth_table[i].sock);
#endif
						//The latest IM on-line client should be updated to all on-line clients.
						update_IM = true;
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Disconnect from client %s : %u", inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
							write_log(log_buffer, "");
						}
						//reset
						auth_table[i].IM_online = false;
						auth_table[i].sock = 0;
						memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
					}
					else if (rtn_val == 4)
					{
						//heartbeat
						if (record_log)
						{
							//write log
							write_log(auth_table[i].id, &(auth_table[i].addr), true, true, NULL);
						}
					}
					else
					{
						//split package
						pkg.split();
						if (record_log)
						{
							//write log
							write_log(auth_table[i].id, &(auth_table[i].addr), 1, false, &pkg);
						}
						switch (pkg.type)
						{
						case im_package::tp_login:
							switch (pkg.information)
							{
							case im_package::lg_logout_request:
								if (record_log)
								{
									//write log
									sprintf(log_buffer, "Receive logout request from client %u (%s : %d)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), ntohs(auth_table[i].addr.sin_port));
									write_log(log_buffer, "");
								}
								//pack
								memset(&pkg, 0, im_package::size_head);
								//pkg.sender
								pkg.type = im_package::tp_login;
								pkg.information = im_package::lg_logout;
								//pkg.receiver_number
								//pkg.receiver
								//pkg.content_lenth
								pkg.build();
								//send
								send_confirm(auth_table[i].sock, pkg.buffer, im_package::size_head, 0);
								if (record_log)
								{
									//write log
									write_log(auth_table[i].id, &(auth_table[i].addr), 2, false, &pkg);
								}
#ifdef WINDOWS
								//wait for a period of time to make sure that the last package has been sent out
								Sleep(wait_sec);
								//close socket
								closesocket(auth_table[i].sock);
#else
								//wait for a period of time to make sure that the last package has been sent out
								usleep(wait_sec * 1000);
								//close socket
								close(auth_table[i].sock);
#endif
								if (record_log)
								{
									//write log
									sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
									write_log(log_buffer, "");
								}
								//reset
								auth_table[i].IM_online = false;
								auth_table[i].sock = 0;
								memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
								//The latest IM on-line client should be updated to all on-line clients.
								update_IM = true;
								break;
							case im_package::lg_ask:
							case im_package::lg_answer:
							case im_package::lg_accept:
							case im_package::lg_reject_password:
							case im_package::lg_reject_username:
							case im_package::lg_reject_format:
							case im_package::lg_logout:
							case im_package::lg_logout_force:
								//Client should not send this type of package.
								if (record_log)
								{
									//write log
									write_log("Client should not send this type of package.", "");
								}
								break;
							default:
								//The format of package is incorrect.
								if (record_log)
								{
									//write log
									write_log("The format of package is incorrect.", "");
								}
								break;
							}
							break;
						case im_package::tp_table:
							switch (pkg.information)
							{
							case im_package::tb_all:
								if (record_log)
								{
									//write log
									sprintf(log_buffer, "Receive client table request from client %u (%s : %d)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), ntohs(auth_table[i].addr.sin_port));
									write_log(log_buffer, "");
								}
								//pack
								memset(&pkg, 0, im_package::size_head);
								//pkg.sender
								pkg.type = im_package::tp_table;
								pkg.information = im_package::tb_all;
								//pkg.receiver_number
								//pkg.receiver
								temp_ptr_uint32_t = (uint32_t *)pkg.content;
								for (unsigned int j = 0; j < auth_table.size(); ++j)
								{
									temp_ptr_uint32_t[j] = htonl(auth_table[j].id);
								}
								pkg.content_lenth = (auth_table.size() << 2);
								pkg.build();
								//send
								rtn_val = send_confirm(auth_table[i].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
								if (rtn_val == -1)
								{
									if (record_log)
									{
										//write log
#ifdef WINDOWS
										write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
										write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
									}
									//close socket
#ifdef WINDOWS
									closesocket(auth_table[i].sock);
#else
									close(auth_table[i].sock);
#endif
									if (record_log)
									{
										//write log
										sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
										write_log(log_buffer, "");
									}
									//reset
									auth_table[i].IM_online = false;
									auth_table[i].sock = 0;
									memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
									//The latest IM on-line client should be updated to all on-line clients.
									update_IM = true;
								}
								else
								{
									if (record_log)
									{
										//write log
										write_log(auth_table[i].id, &(auth_table[i].addr), 2, false, &pkg);
									}
									if (record_log)
									{
										//write log
										write_log("Repeat.", "");
									}
								}
								break;
							case im_package::tb_online:
								if (record_log)
								{
									//write log
									sprintf(log_buffer, "Receive IM online client table request from client %u (%s : %d)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), ntohs(auth_table[i].addr.sin_port));
									write_log(log_buffer, "");
								}
								//pack
								memset(&pkg, 0, im_package::size_head);
								//pkg.sender
								pkg.type = im_package::tp_table;
								pkg.information = im_package::tb_online;
								//pkg.receiver_number
								//pkg.receiver
								temp_ptr_uint32_t = (uint32_t *)pkg.content;
								temp_uint32_t = 0;
								for (unsigned int j = 0; j < auth_table.size(); ++j)
								{
									if (auth_table[j].IM_online)
									{
										temp_ptr_uint32_t[temp_uint32_t] = htonl(auth_table[j].id);
										temp_uint32_t++;
									}
								}
								pkg.content_lenth = (temp_uint32_t << 2);
								pkg.build();
								//send
								rtn_val = send_confirm(auth_table[i].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
								if (rtn_val == -1)
								{
									if (record_log)
									{
										//write log
#ifdef WINDOWS
										write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
										write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
									}
									//close socket
#ifdef WINDOWS
									closesocket(auth_table[i].sock);
#else
									close(auth_table[i].sock);
#endif
									if (record_log)
									{
										//write log
										sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
										write_log(log_buffer, "");
									}
									//reset
									auth_table[i].IM_online = false;
									auth_table[i].sock = 0;
									memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
									//The latest IM on-line client should be updated to all on-line clients.
									update_IM = true;
								}
								else
								{
									if (record_log)
									{
										//write log
										write_log(auth_table[i].id, &(auth_table[i].addr), 2, false, &pkg);
									}
									if (record_log)
									{
										//write log
										write_log("Repeat.", "");
									}
								}
								break;
							default:
								//The format of package is incorrect.
								if (record_log)
								{
									//write log
									write_log("The format of package is incorrect.", "");
								}
								break;
							}
							break;
						case im_package::tp_text:
							switch (pkg.information)
							{
							case im_package::tx_content:
								//forward
								if (pkg.receiver_number == 0xFFFFFFFF)
								{
									//forward to all online client
									for (unsigned int j = 0; j < auth_table.size(); ++j)
									{
										if (auth_table[j].IM_online)
										{
											//send
											rtn_val = send_confirm(auth_table[j].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
											if (rtn_val == -1)
											{
												if (record_log)
												{
													//write log
#ifdef WINDOWS
													write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
													write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
												}
												//close socket
#ifdef WINDOWS
												closesocket(auth_table[j].sock);
#else
												close(auth_table[j].sock);
#endif
												if (record_log)
												{
													//write log
													sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[j].id, inet_ntoa(auth_table[j].addr.sin_addr), auth_table[j].addr.sin_port);
													write_log(log_buffer, "");
												}
												//reset
												auth_table[j].IM_online = false;
												auth_table[j].sock = 0;
												memset(&(auth_table[j].addr), 0, sizeof(auth_table[j].addr));
												//The latest IM on-line client should be updated to all on-line clients.
												update_IM = true;
											}
											else
											{
												if (record_log)
												{
													//write log
													write_log(auth_table[j].id, &(auth_table[j].addr), 3, false, &pkg);
												}
											}
										}
									}
								}
								else
								{
									//forward to specified client
									for (unsigned int k = 0; k < pkg.receiver_number; ++k)
									{
										for (unsigned int j = 0; j < auth_table.size(); ++j)
										{
											if (auth_table[j].id == pkg.receiver[k] && auth_table[j].IM_online)
											{
												//send
												rtn_val = send_confirm(auth_table[j].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
												if (rtn_val == -1)
												{
													if (record_log)
													{
														//write log
#ifdef WINDOWS
														write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
														write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
													}
													//close socket
#ifdef WINDOWS
													closesocket(auth_table[j].sock);
#else
													close(auth_table[j].sock);
#endif
													if (record_log)
													{
														//write log
														sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[j].id, inet_ntoa(auth_table[j].addr.sin_addr), auth_table[j].addr.sin_port);
														write_log(log_buffer, "");
													}
													//reset
													auth_table[j].IM_online = false;
													auth_table[j].sock = 0;
													memset(&(auth_table[j].addr), 0, sizeof(auth_table[j].addr));
													//The latest IM on-line client should be updated to all on-line clients.
													update_IM = true;
												}
												else
												{
													if (record_log)
													{
														//write log
														write_log(auth_table[j].id, &(auth_table[j].addr), 3, false, &pkg);
													}
												}
											}
										}
									}
								}
								//reply
								//pack
								memset(&pkg, 0, im_package::size_head);
								//pkg.sender
								pkg.type = im_package::tp_text;
								pkg.information = im_package::tx_reply;
								//pkg.receiver_number
								//pkg.receiver
								//pkg.content_lenth
								pkg.build();
								//send
								rtn_val = send_confirm(auth_table[i].sock, pkg.buffer, im_package::size_head, 0);
								if (rtn_val == -1)
								{
									if (record_log)
									{
										//write log
#ifdef WINDOWS
										write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
										write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
									}
									//close socket
#ifdef WINDOWS
									closesocket(auth_table[i].sock);
#else
									close(auth_table[i].sock);
#endif
									if (record_log)
									{
										//write log
										sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
										write_log(log_buffer, "");
									}
									//reset
									auth_table[i].IM_online = false;
									auth_table[i].sock = 0;
									memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
									//The latest IM on-line client should be updated to all on-line clients.
									update_IM = true;
								}
								else
								{
									if (record_log)
									{
										//write log
										write_log(auth_table[i].id, &(auth_table[i].addr), 2, false, &pkg);
									}
								}
								break;
							case im_package::tx_reply:
								//Client should not send this type of package.
								if (record_log)
								{
									//write log
									write_log("Client should not send this type of package.", "");
								}
								break;
							default:
								//The format of package is incorrect.
								if (record_log)
								{
									//write log
									write_log("The format of package is incorrect.", "");
								}
								break;
							}
							break;
						case im_package::tp_file:
							switch (pkg.information)
							{
							case im_package::fl_content:
							case im_package::fl_reply:
							case im_package::fl_request:
							case im_package::fl_accept:
							case im_package::fl_reject:
							case im_package::fl_interrupt:
							case im_package::fl_finish:
								if (pkg.receiver_number == 1)
								{
									//forward
									temp_bool_1 = false;
									for (unsigned int j = 0; j < auth_table.size(); ++j)
									{
										if (auth_table[j].id == pkg.receiver[0])
										{
											if (auth_table[j].IM_online)
											{
												//send
												rtn_val = send_confirm(auth_table[j].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
												if (rtn_val == -1)
												{
													if (record_log)
													{
														//write log
#ifdef WINDOWS
														write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
														write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
													}
													//close socket
#ifdef WINDOWS
													closesocket(auth_table[j].sock);
#else
													close(auth_table[j].sock);
#endif
													if (record_log)
													{
														//write log
														sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[j].id, inet_ntoa(auth_table[j].addr.sin_addr), auth_table[j].addr.sin_port);
														write_log(log_buffer, "");
													}
													//reset
													auth_table[j].IM_online = false;
													auth_table[j].sock = 0;
													memset(&(auth_table[j].addr), 0, sizeof(auth_table[j].addr));
													//The latest IM on-line client should be updated to all on-line clients.
													update_IM = true;
													temp_bool_1 = false;
												}
												else
												{
													if (record_log)
													{
														//write log
														write_log(auth_table[j].id, &(auth_table[j].addr), 3, false, &pkg);
													}
													temp_bool_1 = true;
												}
											}
											else
											{
												temp_bool_1 = false;
											}
											break;
										}
										else
										{
											temp_bool_1 = false;
										}
									}
									if (temp_bool_1 == false)
									{
										//offline
										//pack
										memset(&pkg, 0, im_package::size_head);
										//pkg.sender
										pkg.type = im_package::tp_file;
										pkg.information = im_package::fl_offline;
										//pkg.receiver_number
										//pkg.receiver
										//pkg.content_lenth
										pkg.build();
										//send
										rtn_val = send_confirm(auth_table[i].sock, pkg.buffer, im_package::size_head, 0);
										if (rtn_val == -1)
										{
											if (record_log)
											{
												//write log
#ifdef WINDOWS
												write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
												write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
											}
											//close socket
#ifdef WINDOWS
											closesocket(auth_table[i].sock);
#else
											close(auth_table[i].sock);
#endif
											if (record_log)
											{
												//write log
												sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
												write_log(log_buffer, "");
											}
											//reset
											auth_table[i].IM_online = false;
											auth_table[i].sock = 0;
											memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
											//The latest IM on-line client should be updated to all on-line clients.
											update_IM = true;
										}
										else
										{
											if (record_log)
											{
												//write log
												write_log(auth_table[i].id, &(auth_table[i].addr), 2, false, &pkg);
											}
										}
									}
								}
								break;
							case im_package::fl_offline:
								//Client should not send this type of package.
								if (record_log)
								{
									//write log
									write_log("Client should not send this type of package.", "");
								}
								break;
							default:
								//The format of package is incorrect.
								if (record_log)
								{
									//write log
									write_log("The format of package is incorrect.", "");
								}
								break;
							}
							break;
						case im_package::tp_doudizhu:
							switch (pkg.information)
							{
							case im_package::dz_online:
								if (record_log)
								{
									//write log
									sprintf(log_buffer, "Receive doudizhu online client table request from client %u (%s : %d)", inet_ntoa(auth_table[i].addr.sin_addr), ntohs(auth_table[i].addr.sin_port));
									write_log(log_buffer, "");
								}
								//pack
								memset(&pkg, 0, im_package::size_head);
								//pkg.sender
								pkg.type = im_package::tp_doudizhu;
								pkg.information = im_package::dz_online;
								//pkg.receiver_number
								//pkg.receiver
								temp_uint32_t = 0;
								for (unsigned j = 0; j < auth_table.size(); ++j)
								{
									if (auth_table[j].doudizhu_online)
									{
										*((uint32_t *)(pkg.content + (temp_uint32_t << 2))) = auth_table[j].id;
										temp_uint32_t++;
									}
								}
								pkg.content_lenth = (temp_uint32_t << 2);
								pkg.build();
								//send
								rtn_val = send_confirm(auth_table[i].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
								if (rtn_val == -1)
								{
									if (record_log)
									{
										//write log
#ifdef WINDOWS
										write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
										write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
									}
									//close socket
#ifdef WINDOWS
									closesocket(auth_table[i].sock);
#else
									close(auth_table[i].sock);
#endif
									if (record_log)
									{
										//write log
										sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[i].id, inet_ntoa(auth_table[i].addr.sin_addr), auth_table[i].addr.sin_port);
										write_log(log_buffer, "");
									}
									//reset
									auth_table[i].IM_online = false;
									auth_table[i].sock = 0;
									memset(&(auth_table[i].addr), 0, sizeof(auth_table[i].addr));
									//The latest IM on-line client should be updated to all on-line clients.
									update_IM = true;
								}
								else
								{
									if (record_log)
									{
										//write log
										write_log(auth_table[i].id, &(auth_table[i].addr), 2, false, &pkg);
									}
									if (record_log)
									{
										//write log
										write_log("Repeat.", "");
									}
								}
								break;
							case im_package::dz_accept:
#ifdef PIPE
								if (pipe_status == 1)
								{
									//prepare
									pipe_buf_ptr[0] = 20;
									pipe_buf_ptr[1] = 4;
									pipe_buf_ptr[2] = pkg.sender;
									pipe_buf_ptr[3] = pkg.receiver[18];
									//reply
									pipe_buf_ptr[4] = 3;
									//write fd_pipe_I2D
									rtn_val = write_confirm(fd_pipe_I2D, pipe_buf, pipe_buf_ptr[0]);
									if (rtn_val <= 0)
									{
										if (record_log)
										{
											//write log
											strcpy(log_buffer, "Fail to write ");
											strcat(log_buffer, pipe_I2D);
											strcat(log_buffer, ".");
											write_log(log_buffer, strerror(errno));
										}
									}
									else
									{
										if (record_log)
										{
											//write log
											write_log("Forward doudizhu reply (accept) from client to pipe_I2D.", "");
										}
										if (record_log)
										{
											//write log
											write_log(pipe_buf_ptr);
										}
									}
									//write fd_pipe_I2D_r
									rtn_val = write_confirm(fd_pipe_I2D_r, pipe_buf, 1);
									if (rtn_val <= 0)
									{
										if (record_log)
										{
											//write log
											strcpy(log_buffer, "Fail to write ");
											strcat(log_buffer, pipe_I2D_r);
											strcat(log_buffer, ".");
											write_log(log_buffer, strerror(errno));
										}
									}
								}
#endif
								break;
							case im_package::dz_reject:
#ifdef PIPE
								if (pipe_status == 1)
								{
									//prepare
									pipe_buf_ptr[0] = 20;
									pipe_buf_ptr[1] = 4;
									pipe_buf_ptr[2] = pkg.sender;
									pipe_buf_ptr[3] = pkg.receiver[18];
									//reply
									pipe_buf_ptr[4] = 2;
									//write fd_pipe_I2D
									rtn_val = write_confirm(fd_pipe_I2D, pipe_buf, pipe_buf_ptr[0]);
									if (rtn_val <= 0)
									{
										if (record_log)
										{
											//write log
											strcpy(log_buffer, "Fail to write ");
											strcat(log_buffer, pipe_I2D);
											strcat(log_buffer, ".");
											write_log(log_buffer, strerror(errno));
										}
									}
									else
									{
										if (record_log)
										{
											//write log
											write_log("Forward doudizhu reply (reject) from client to pipe_I2D.", "");
										}
										if (record_log)
										{
											//write log
											write_log(pipe_buf_ptr);
										}
									}
									//write fd_pipe_I2D_r
									rtn_val = write_confirm(fd_pipe_I2D_r, pipe_buf, 1);
									if (rtn_val <= 0)
									{
										if (record_log)
										{
											//write log
											strcpy(log_buffer, "Fail to write ");
											strcat(log_buffer, pipe_I2D_r);
											strcat(log_buffer, ".");
											write_log(log_buffer, strerror(errno));
										}
									}
								}
#endif
								break;
							case im_package::dz_invite:
								//Client should not send this type of package.
								if (record_log)
								{
									//write log
									write_log("Client should not send this type of package.", "");
								}
								break;
							default:
								//The format of package is incorrect.
								if (record_log)
								{
									//write log
									write_log("The format of package is incorrect.", "");
								}
								break;
							}
							break;
						default:
							//The format of package is incorrect.
							if (record_log)
							{
								//write log
								write_log("The format of package is incorrect.", "");
							}
							break;
						}
					}
				}
			}
		}

		//Clear time-out client in G2.
		curr_time = time(NULL);
		if (curr_time - last_conn_time > login_timeout)
		{
			for (unsigned int i = 0; i <conn_table.size(); ++i)
			{
				if (conn_table[i].check_vaild(login_timeout) == false)
				{
					//close socket
#ifdef WINDOWS
					closesocket(conn_table[i].sock);
#else
					close(conn_table[i].sock);
#endif
					//remove item in "conn_table"
					conn_table.erase(conn_table.begin() + i);
				}
			}
			last_conn_time = time(NULL);
		}

		//If on-line client table has been changed, update_IM it to doudizhu server and all on-line clients.
		if (update_IM)
		{
			update_IM = false;
			if (record_log)
			{
				//write log
				write_log("Update IM on-line client table to all on-line clients.", "");
			}
			//pack
			memset(&pkg, 0, im_package::size_head);
			//pkg.sender
			pkg.type = im_package::tp_table;
			pkg.information = im_package::tb_online;
			//pkg.receiver_number
			//pkg.receiver
			temp_ptr_uint32_t = (uint32_t *)pkg.content;
			temp_uint32_t = 0;
			for (unsigned int j = 0; j < auth_table.size(); ++j)
			{
				if (auth_table[j].IM_online)
				{
					temp_ptr_uint32_t[temp_uint32_t] = htonl(auth_table[j].id);
					temp_uint32_t++;
				}
			}
			pkg.content_lenth = (temp_uint32_t << 2);
			pkg.build();
			//send to all online client
			for (unsigned int j = 0; j < auth_table.size(); ++j)
			{
				if (auth_table[j].IM_online)
				{
					//send
					rtn_val = send_confirm(auth_table[j].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
					if (rtn_val == -1)
					{
						if (record_log)
						{
							//write log
#ifdef WINDOWS
							write_log("Client, which has not closed it's socket yet, disconnected.", "");
#else
							write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
#endif
						}
						//close socket
#ifdef WINDOWS
						closesocket(auth_table[j].sock);
#else
						close(auth_table[j].sock);
#endif
						if (record_log)
						{
							//write log
							sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[j].id, inet_ntoa(auth_table[j].addr.sin_addr), auth_table[j].addr.sin_port);
							write_log(log_buffer, "");
						}
						//reset
						auth_table[j].IM_online = false;
						auth_table[j].sock = 0;
						memset(&(auth_table[j].addr), 0, sizeof(auth_table[j].addr));
						//The latest IM on-line client should be updated to all on-line clients.
						update_IM = true;
					}
					else
					{
						if (record_log)
						{
							//write log
							write_log(auth_table[j].id, &(auth_table[j].addr), 2, false, &pkg);
						}
					}
				}
			}
			if (record_log)
			{
				//write log
				write_log("Complete.", "");
			}
			if (record_log)
			{
				//write log
				write_log(&auth_table, 1);
			}
#ifdef PIPE
			if (pipe_status == 1)
			{
				//prepare
				temp_uint32_t = 0;
				for (unsigned int j = 0; j < auth_table.size(); ++j)
				{
					if (auth_table[j].IM_online)
					{
						pipe_buf_ptr_table[temp_uint32_t] = auth_table[j].id;
						temp_uint32_t++;
					}
				}
				pipe_buf_ptr[2] = temp_uint32_t;
				pipe_buf_ptr[1] = 1;
				pipe_buf_ptr[0] = (pipe_buf_ptr[2] + 3) << 2;
				//write fd_pipe_I2D
				rtn_val = write_confirm(fd_pipe_I2D, pipe_buf, pipe_buf_ptr[0]);
				if (rtn_val <= 0)
				{
					if (record_log)
					{
						//write log
						strcpy(log_buffer, "Fail to write ");
						strcat(log_buffer, pipe_I2D);
						strcat(log_buffer, ".");
						write_log(log_buffer, strerror(errno));
					}
				}
				else
				{
					if (record_log)
					{
						//write log
						write_log("Send data to pipe_I2D: IM online client table.", "");
					}
					if (record_log)
					{
						//write log
						write_log(pipe_buf_ptr);
					}
				}
				//write fd_pipe_I2D_r
				rtn_val = write_confirm(fd_pipe_I2D_r, pipe_buf, 1);
				if (rtn_val <= 0)
				{
					if (record_log)
					{
						//write log
						strcpy(log_buffer, "Fail to write ");
						strcat(log_buffer, pipe_I2D_r);
						strcat(log_buffer, ".");
						write_log(log_buffer, strerror(errno));
					}
				}
				else
				{
					if (record_log)
					{
						//write log
						write_log("Complete.", "");
					}
				}
			}
#endif
		}

#ifdef PIPE
		if (update_doudizhu)
		{
			if (pipe_status == 1)
			{
				update_doudizhu = false;
				if (record_log)
				{
					//write log
					write_log("Update doudizhu on-line client table to all on-line clients.", "");
				}
				//pack
				memset(&pkg, 0, im_package::size_head);
				//pkg.sender
				pkg.type = im_package::tp_doudizhu;
				pkg.information = im_package::dz_online;
				//pkg.receiver_number
				//pkg.receiver
				temp_ptr_uint32_t = (uint32_t *)pkg.content;
				temp_uint32_t = 0;
				for (unsigned int j = 0; j < auth_table.size(); ++j)
				{
					if (auth_table[j].doudizhu_online)
					{
						temp_ptr_uint32_t[temp_uint32_t] = htonl(auth_table[j].id);
						temp_uint32_t++;
					}
				}
				pkg.content_lenth = (temp_uint32_t << 2);
				pkg.build();
				//send to all online client
				for (unsigned int j = 0; j < auth_table.size(); ++j)
				{
					if (auth_table[j].IM_online)
					{
						//send
						rtn_val = send_confirm(auth_table[j].sock, pkg.buffer, im_package::size_head + pkg.preread(), 0);
						if (rtn_val == -1)
						{
							if (record_log)
							{
								//write log
								write_log("Client, which has not closed it's socket yet, disconnected.", strerror(errno));
							}
							//close socket
							close(auth_table[j].sock);
							if (record_log)
							{
								//write log
								sprintf(log_buffer, "Disconnect from client %u (%s : %u)", auth_table[j].id, inet_ntoa(auth_table[j].addr.sin_addr), auth_table[j].addr.sin_port);
								write_log(log_buffer, "");
							}
							//reset
							auth_table[j].IM_online = false;
							auth_table[j].sock = 0;
							memset(&(auth_table[j].addr), 0, sizeof(auth_table[j].addr));
							//The latest IM on-line client should be updated to all on-line clients.
							update_IM = true;
						}
						else
						{
							if (record_log)
							{
								//write log
								write_log(auth_table[j].id, &(auth_table[j].addr), 2, false, &pkg);
							}
						}
					}
				}
				if (record_log)
				{
					//write log
					write_log("Complete.", "");
				}
				if (record_log)
				{
					//write log
					write_log(&auth_table, 1);
				}
			}
		}
#endif

	}
}

//write log: application message
void write_log(char * application_message, char * error_string)
{
	curr_time = time(NULL);
	if (console)
	{
		printf("<Record>\n");
		printf("\t<Time>\n");
		printf("\t\t%s", ctime(&curr_time));
		printf("\t</Time>\n");
		printf("\t<Application>\n");
		printf("\t\t%s %s\n", application_message, error_string);
		printf("\t</Application>\n");
		printf("</Record>\n");
		printf("\n");
	}
	else
	{
		fprintf(fd_log, "<Record>\n");
		fprintf(fd_log, "\t<Time>\n");
		fprintf(fd_log, "\t\t%s", ctime(&curr_time));
		fprintf(fd_log, "\t</Time>\n");
		fprintf(fd_log, "\t<Application>\n");
		fprintf(fd_log, "\t\t%s %s\n", application_message, error_string);
		fprintf(fd_log, "\t</Application>\n");
		fprintf(fd_log, "</Record>\n");
		fprintf(fd_log, "\n");
		fflush(fd_log);
	}
}

//write log: client table
void write_log(vector<auth_item> * auth_table, int type)
{
	curr_time = time(NULL);
	if (console)
	{
		printf("<Record>\n");
		printf("\t<Time>\n");
		printf("\t\t%s", ctime(&curr_time));
		printf("\t</Time>\n");
		printf("\t<Client Table>\n");
		switch (type)
		{
			case 0:
				//all user
				printf("\t\t<type>\n");
				printf("\t\t\tall\n");
				printf("\t\t</type>\n");
				printf("\t\t<item>\n");
				for (unsigned int i = 0; i < auth_table->size(); ++i)
				{
					printf("\t\t\t%u\n", (*auth_table)[i].id);
				}
				printf("\t\t</item>\n");
				break;
			case 1:
				//IM online user
				printf("\t\t<type>\n");
				printf("\t\t\tIM online\n");
				printf("\t\t</type>\n");
				printf("\t\t<item>\n");
				for (unsigned int i = 0; i < auth_table->size(); ++i)
				{
					if ((*auth_table)[i].IM_online)
					{
						printf("\t\t\t%u\n", (*auth_table)[i].id);
					}
				}
				printf("\t\t</item>\n");
				break;
			case 2:
				//doudizhu online user
				printf("\t\t<type>\n");
				printf("\t\t\tdoudizhu online\n");
				printf("\t\t</type>\n");
				printf("\t\t<item>\n");
				for (unsigned int i = 0; i < auth_table->size(); ++i)
				{
					if ((*auth_table)[i].doudizhu_online)
					{
						printf("\t\t\t%u\n", (*auth_table)[i].id);
					}
				}
				printf("\t\t</item>\n");
				break;
			default:
				break;
		}
		printf("\t</Client Table>\n");
		printf("</Record>\n");
		printf("\n");
	}
	else
	{
		fprintf(fd_log, "<Record>\n");
		fprintf(fd_log, "\t<Time>\n");
		fprintf(fd_log, "\t\t%s", ctime(&curr_time));
		fprintf(fd_log, "\t</Time>\n");
		fprintf(fd_log, "\t<Client Table>\n");
		switch (type)
		{
		case 0:
			//all user
			fprintf(fd_log, "\t\t<type>\n");
			fprintf(fd_log, "\t\t\tall\n");
			fprintf(fd_log, "\t\t</type>\n");
			fprintf(fd_log, "\t\t<item>\n");
			for (unsigned int i = 0; i < auth_table->size(); ++i)
			{
				fprintf(fd_log, "\t\t\t%u\n", (*auth_table)[i].id);
			}
			fprintf(fd_log, "\t\t</item>\n");
			break;
		case 1:
			//IM online user
			fprintf(fd_log, "\t\t<type>\n");
			fprintf(fd_log, "\t\t\tIM online\n");
			fprintf(fd_log, "\t\t</type>\n");
			fprintf(fd_log, "\t\t<item>\n");
			for (unsigned int i = 0; i < auth_table->size(); ++i)
			{
				if ((*auth_table)[i].IM_online)
				{
					fprintf(fd_log, "\t\t\t%u\n", (*auth_table)[i].id);
				}
			}
			fprintf(fd_log, "\t\t</item>\n");
			break;
		case 2:
			//doudizhu online user
			fprintf(fd_log, "\t\t<type>\n");
			fprintf(fd_log, "\t\t\tdoudizhu online\n");
			fprintf(fd_log, "\t\t</type>\n");
			fprintf(fd_log, "\t\t<item>\n");
			for (unsigned int i = 0; i < auth_table->size(); ++i)
			{
				if ((*auth_table)[i].doudizhu_online)
				{
					fprintf(fd_log, "\t\t\t%u\n", (*auth_table)[i].id);
				}
			}
			fprintf(fd_log, "\t\t</item>\n");
			break;
		default:
			break;
		}
		fprintf(fd_log, "\t</Client Table>\n");
		fprintf(fd_log, "</Record>\n");
		fprintf(fd_log, "\n");
		fflush(fd_log);
	}
}

//write log: package
void write_log(uint32_t username, sockaddr_in * endpoint, int communication, bool heartbeat, im_package * package)
{
	if (record_log_file_transmission == false)
	{
		if (heartbeat == false)
		{
			if (package->type == im_package::tp_file)
			{
				if (package->information == im_package::fl_content)
				{
					return;
				}
				else if (package->information == im_package::fl_reply)
				{
					return;
				}
			}
		}
	}
	curr_time = time(NULL);
	if (console)
	{
		printf("<Record>\n");
		printf("\t<Time>\n");
		printf("\t\t%s", ctime(&curr_time));
		printf("\t</Time>\n");
		printf("\t<Communication>\n");
		switch (communication)
		{
		case 1:	//reveive
			printf("\t\tReceive from ");
			break;
		case 2:	//send
			if (package != NULL)
			{
				package->ntoh_head();
			}
			printf("\t\tSend to ");
			break;
		case 3:	//forward
			printf("\t\tForward to ");
			break;
		default:
			break;
		}
		if (username == 0)
		{
			printf("%s : %u\n", inet_ntoa(endpoint->sin_addr), endpoint->sin_port);
		}
		else
		{
			printf("%d (%s : %u)\n", username, inet_ntoa(endpoint->sin_addr), endpoint->sin_port);
		}
		printf("\t</Communication>\n");
		if (heartbeat)
		{
			printf("\t<Heartbeat>\n");
			printf("\t\t0xFFFFFFFF\n");
			printf("\t</Heartbeat>\n");
		}
		else
		{
			printf("\t<Package>\n");
			printf("\t\t<sender> %u </sender>\n", package->sender);
			printf("\t\t<type> %u </type>\n", package->type);
			printf("\t\t<information> %u </information>\n", package->information);
			if (package->receiver_number != 0xFFFFFFFF)
			{
				printf("\t\t<receiver_number> %u </receiver_number>\n", package->receiver_number);
				for (unsigned int i = 0; i < package->receiver_number; ++i)
				{
					printf("\t\t<receiver> %u %u </receiver>\n", i, package->receiver[i]);
				}
			}
			else
			{
				printf("\t\t<receiver_number> 0xFFFFFFFF </receiver_number>\n", package->receiver_number);
			}
			printf("\t\t<content_lenth> %u </content_lenth>\n", package->content_lenth);
			if (package->content_lenth != 0)
			{
				if
					(
						(package->type == im_package::tp_table) ||
						(package->type == im_package::tp_doudizhu && package->information == im_package::dz_online)
					)
				{
					printf("\t\t<content>\n");
					for (unsigned int i = 0; i < package->content_lenth; i += 4)
					{
						printf("\t\t\t%u\n", ntohl(*((int *)((package->content) + i))));
					}
					printf("\t\t</content>\n");
				}
				else
				{
					if (package->type == im_package::tp_file && package->information == im_package::fl_content)
					{
						//nothing
					}
					else
					{
						if (package->type == im_package::tp_file && package->information == im_package::fl_request)
						{
							strncpy(log_buffer, (package->content) + 8, (package->content_lenth) - 8);
							log_buffer[(package->content_lenth) - 8] = '\0';
						}
						else
						{
							strncpy(log_buffer, package->content, package->content_lenth);
						}
						if ((package->content_lenth) == im_package::size_content_max)
						{
							log_buffer[im_package::size_content_max - 1] = '\0';
						}
						else
						{
							log_buffer[package->content_lenth] = '\0';
						}
						printf("\t\t<content>\n");
						printf("\t\t\t%s\n", log_buffer);
						printf("\t\t</content>\n");
					}
				}
			}
			printf("\t</Package>\n");
		}
		printf("</Record>\n");
		printf("\n");
		switch (communication)
		{
		case 1:	//reveive
			break;
		case 2:	//send
			if (package != NULL)
			{
				package->hton_head();
			}
			break;
		case 3:	//forward
			break;
		default:
			break;
		}
	}
	else
	{
		fprintf(fd_log, "<Record>\n");
		fprintf(fd_log, "\t<Time>\n");
		fprintf(fd_log, "\t\t%s", ctime(&curr_time));
		fprintf(fd_log, "\t</Time>\n");
		fprintf(fd_log, "\t<Communication>\n");
		switch (communication)
		{
		case 1:	//reveive
			fprintf(fd_log, "\t\tReceive from ");
			break;
		case 2:	//send
			if (package != NULL)
			{
				package->ntoh_head();
			}
			fprintf(fd_log, "\t\tSend to ");
			break;
		case 3:	//forward
			fprintf(fd_log, "\t\tForward to ");
			break;
		default:
			break;
		}
		if (username == 0)
		{
			fprintf(fd_log, "%s : %u\n", inet_ntoa(endpoint->sin_addr), endpoint->sin_port);
		}
		else
		{
			fprintf(fd_log, "%d (%s : %u)\n", username, inet_ntoa(endpoint->sin_addr), endpoint->sin_port);
		}
		fprintf(fd_log, "\t</Communication>\n");
		if (heartbeat)
		{
			fprintf(fd_log, "\t<Heartbeat>\n");
			fprintf(fd_log, "\t\t0xFFFFFFFF\n");
			fprintf(fd_log, "\t</Heartbeat>\n");
		}
		else
		{
			fprintf(fd_log, "\t<Package>\n");
			fprintf(fd_log, "\t\t<sender> %u </sender>\n", package->sender);
			fprintf(fd_log, "\t\t<type> %u </type>\n", package->type);
			fprintf(fd_log, "\t\t<information> %u </information>\n", package->information);
			if (package->receiver_number != 0xFFFFFFFF)
			{
				fprintf(fd_log, "\t\t<receiver_number> %u </receiver_number>\n", package->receiver_number);
				for (unsigned int i = 0; i < package->receiver_number; ++i)
				{
					fprintf(fd_log, "\t\t<receiver> %u %u </receiver>\n", i, package->receiver[i]);
				}
			}
			else
			{
				fprintf(fd_log, "\t\t<receiver_number> 0xFFFFFFFF </receiver_number>\n", package->receiver_number);
			}
			fprintf(fd_log, "\t\t<content_lenth> %u </content_lenth>\n", package->content_lenth);
			if (package->content_lenth != 0)
			{
				if
					(
					(package->type == im_package::tp_table) ||
					(package->type == im_package::tp_doudizhu && package->information == im_package::dz_online)
					)
				{
					fprintf(fd_log, "\t\t<content>\n");
					for (unsigned int i = 0; i < package->content_lenth; i += 4)
					{
						fprintf(fd_log, "\t\t\t%u\n", ntohl(*((int *)((package->content) + i))));
					}
					fprintf(fd_log, "\t\t</content>\n");
				}
				else
				{
					if (package->type == im_package::tp_file && package->information == im_package::fl_content)
					{
						//nothing
					}
					else
					{
						if (package->type == im_package::tp_file && package->information == im_package::fl_request)
						{
							strncpy(log_buffer, (package->content) + 8, (package->content_lenth) - 8);
							log_buffer[(package->content_lenth) - 8] = '\0';
						}
						else
						{
							strncpy(log_buffer, package->content, package->content_lenth);
						}
						if ((package->content_lenth) == im_package::size_content_max)
						{
							log_buffer[im_package::size_content_max - 1] = '\0';
						}
						else
						{
							log_buffer[package->content_lenth] = '\0';
						}
						fprintf(fd_log, "\t\t<content>\n");
						fprintf(fd_log, "\t\t\t%s\n", log_buffer);
						fprintf(fd_log, "\t\t</content>\n");
					}
				}
			}
			fprintf(fd_log, "\t</Package>\n");
		}
		fprintf(fd_log, "</Record>\n");
		fprintf(fd_log, "\n");
		switch (communication)
		{
		case 1:	//reveive
			break;
		case 2:	//send
			if (package != NULL)
			{
				package->hton_head();
			}
			break;
		case 3:	//forward
			break;
		default:
			break;
		}
		fflush(fd_log);
	}
}

#ifdef PIPE
void write_log(uint32_t * pipe_buffer_uint32_t)
{
	curr_time = time(NULL);
	if (console)
	{
		printf("<Record>\n");
		printf("\t<Time>\n");
		printf("\t\t%s", ctime(&curr_time));
		printf("\t</Time>\n");
		printf("\t<Pipe>\n");
		printf("\t\t<[0]> %u </[0]>\n", pipe_buffer_uint32_t[0]);
		for (uint32_t i = 1; i < pipe_buffer_uint32_t[0] >> 2; ++i)
		{
			printf("\t\t<[%u]> %u </[%u]>\n", i << 2, pipe_buffer_uint32_t[i], i << 2);
		}
		printf("\t</Pipe>\n");
		printf("</Record>\n");
		printf("\n");
	}
	else
	{
		fprintf(fd_log, "<Record>\n");
		fprintf(fd_log, "\t<Time>\n");
		fprintf(fd_log, "\t\t%s", ctime(&curr_time));
		fprintf(fd_log, "\t</Time>\n");
		fprintf(fd_log, "\t<Pipe>\n");
		fprintf(fd_log, "\t\t<[0]> %u </[0]>\n", pipe_buffer_uint32_t[0]);
		for (uint32_t i = 1; i < pipe_buffer_uint32_t[0] >> 2; ++i)
		{
			fprintf(fd_log, "\t\t<[%u]> %u </[%u]>\n", i << 2, pipe_buffer_uint32_t[i], i << 2);
		}
		fprintf(fd_log, "\t</Pipe>\n");
		fprintf(fd_log, "</Record>\n");
		fprintf(fd_log, "\n");
		fflush(fd_log);
	}
}

int read_confirm(int fd, char * buffer, int size)
{
	int lenth;
	int offset;
	int rtn_val;
	for (lenth = size, offset = 0; lenth > 0; lenth -= rtn_val, offset += rtn_val)
	{
		rtn_val = read(fd, buffer + offset, lenth);
		if (rtn_val <= 0)
		{
			return rtn_val;
		}
	}
	return size;
}

int write_confirm(int fd, char * buffer, int size)
{
	int lenth;
	int offset;
	int rtn_val;
	for (lenth = size, offset = 0; lenth > 0; lenth -= rtn_val, offset += rtn_val)
	{
		rtn_val = write(fd, buffer + offset, lenth);
		if (rtn_val <= 0)
		{
			return rtn_val;
		}
	}
	return size;
}
#endif

//receive package
#ifdef WINDOWS
int receive_package(SOCKET sock, im_package * pkg, bool peek)
#else
int receive_package(int sock, im_package * pkg, bool peek)
#endif
{
	int rtn_val;
	int lenth;
	//peek 4 Bit
	while (true)
	{
		rtn_val = recv(sock, pkg->buffer, 4, MSG_PEEK);
		if (rtn_val <= 0)
		{
			return rtn_val;
		}
		else if (rtn_val == 4)
		{
			break;
		}
	}
	if (strncmp(pkg->buffer, "\xFF\xFF\xFF\xFF", 4) == 0)
	{
		//heartbeat
		if (peek == false)
		{
			//receive 4 Bit (heartbeat)
			recv(sock, pkg->buffer, 4, 0);
		}
		return 4;
	}
	else
	{
		//package
		//peek 100 Bit
		while (true)
		{
			rtn_val = recv(sock, pkg->buffer, im_package::size_head, MSG_PEEK);
			if (rtn_val <= 0)
			{
				return rtn_val;
			}
			else if (rtn_val == im_package::size_head)
			{
				break;
			}
		}
		//confirm the lenth of content
		lenth = im_package::size_head + (pkg->preread());
		//peek 100 ~ 65636 Bit
		while (true)
		{
			rtn_val = recv(sock, pkg->buffer, lenth, MSG_PEEK);
			if (rtn_val <= 0)
			{
				return rtn_val;
			}
			else if (rtn_val == lenth)
			{
				break;
			}
		}
		if (peek == false)
		{
			//receive 100 ~ 65636 Bit (package)
			recv(sock, pkg->buffer, lenth, 0);
		}
		return lenth;
	}
}

//make sure to send a number of Bit
#ifdef WINDOWS
int send_confirm(SOCKET sock, char * buffer, int size, int flags)
#else
int send_confirm(int sock, char * buffer, int size, int flags)
#endif
{
	int lenth;
	int offset;
	int rtn_val;
	for (lenth = size, offset = 0; lenth > 0; lenth -= rtn_val, offset += rtn_val)
	{
		rtn_val = send(sock, buffer + offset, lenth, flags);
		if (rtn_val <= 0)
		{
			return rtn_val;
		}
	}
	return size;
}

//process terminate
void terminate(int no)
{
#ifdef PIPE
	//close all pipes
	close(fd_pipe_I2D);
	close(fd_pipe_I2D_r);
	close(fd_pipe_D2I);
	close(fd_pipe_D2I_r);
#endif
	//close all sockets in G1
	for (unsigned int i = 0; i < lis_sock_table.size(); ++i)
	{
#ifdef WINDOWS
		closesocket(lis_sock_table[i]);
#else
		close(lis_sock_table[i]);
#endif
	}
	//close all clients in G2
	for (unsigned int i = 0; i < conn_table.size(); ++i)
	{
#ifdef WINDOWS
		closesocket(conn_table[i].sock);
#else
		close(conn_table[i].sock);
#endif
	}
	//close all clients in G3
	for (unsigned int i = 0; i < auth_table.size(); ++i)
	{
		if (auth_table[i].IM_online)
		{
#ifdef WINDOWS
			closesocket(auth_table[i].sock);
#else
			close(auth_table[i].sock);
#endif
		}
	}
	if (record_log)
	{
		//write log
		sprintf(log_buffer, "All sockets have been closed.");
		write_log(log_buffer, "");
	}
	if (record_log)
	{
		//write log
		sprintf(log_buffer, "Terminate.");
		write_log(log_buffer, "");
	}
	//terminate
	exit(1);
}

#ifdef CAPACITY
#undef CAPACITY
#endif

