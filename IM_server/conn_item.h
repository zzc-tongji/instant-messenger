#ifndef CONN_ITEM_H_
#define CONN_ITEM_H_

#include <time.h>
#include "compiler_option.h"

#ifndef WINDOWS
#include <netinet/in.h>
#else
#include <Winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
#endif

class conn_item
{
public:

	int sock;
	sockaddr_in addr;
	time_t gen_time;
	bool valid;

	void set_tim_val();
	bool check_vaild(int timeout_s);
};

#endif

