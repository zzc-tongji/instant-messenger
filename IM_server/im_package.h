#ifndef IM_PACKAGE_H_
#define IM_PACKAGE_H_

#include <stdint.h>

class im_package
{
public:

	//size of this class
	static const uint32_t size_head = 100;
	static const uint32_t size_content_max = 1024;
	//type
	static const uint32_t tp_login = 1;
	static const uint32_t tp_table = 2;
	static const uint32_t tp_text = 3;
	static const uint32_t tp_file = 4;
	//type - custom
	static const uint32_t tp_doudizhu = 5;
	//information
	//type = tp_login
	static const uint32_t lg_ask = 1;
	static const uint32_t lg_answer = 2;
	static const uint32_t lg_accept = 3;
	static const uint32_t lg_reject_password = 4;
	static const uint32_t lg_reject_username = 5;
	static const uint32_t lg_reject_format = 6;
	static const uint32_t lg_logout_request = 7;
	static const uint32_t lg_logout = 8;
	static const uint32_t lg_logout_force = 9;
	//type = tp_table
	static const uint32_t tb_all = 1;
	static const uint32_t tb_online = 2;
	//type = tp_text
	static const uint32_t tx_content = 1;
	static const uint32_t tx_reply = 2;
	//type = tp_file
	static const uint32_t fl_content = 1;
	static const uint32_t fl_reply = 2;
	static const uint32_t fl_request = 3;
	static const uint32_t fl_accept = 4;
	static const uint32_t fl_reject = 5;
	static const uint32_t fl_interrupt = 6;
	static const uint32_t fl_finish = 7;
	static const uint32_t fl_offline = 8;
	//information - custom
	//type = tp_doudizhu
	static const uint32_t dz_online = 1;
	static const uint32_t dz_invite = 2;
	static const uint32_t dz_accept = 3;
	static const uint32_t dz_reject = 4;

	//head aera
	uint32_t sender;
	uint32_t type;
	uint32_t information;
	uint32_t receiver_number;
	uint32_t receiver[20];
	uint32_t content_lenth;
	//content aera
	char     content[size_content_max];
	//buffer area
	char buffer[size_head + size_content_max];

	//pre-read item "content_lenth" in head aera
	uint32_t preread();
	//split package and ntoh items in head aera
	void split();
	//hton items in head aera and build package
	void build();
	//ntoh items in head aera only
	void ntoh_head();
	//hton items in head aera only
	void hton_head();
};

#endif

