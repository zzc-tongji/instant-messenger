#include "auth_item.h"
#include "conn_item.h"

void auth_item::set_sock_addr(conn_item ci)
{
	sock = ci.sock;
	addr = ci.addr;
}

