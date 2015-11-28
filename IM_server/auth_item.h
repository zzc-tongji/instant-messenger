#ifndef AUTH_ITEM_H_
#define AUTH_ITEM_H_

#include <stdint.h>
#include "compiler_option.h"

#ifndef WINDOWS
#include <netinet/in.h>
#else
#include <Winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
#endif

class conn_item;

class auth_item
{
public:

	uint32_t id;
	char pwd[32];
	bool IM_online;
	bool doudizhu_online;
	int sock;
	sockaddr_in addr;

	void set_sock_addr(conn_item ci);
};

#endif

