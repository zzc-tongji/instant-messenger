#include "conn_item.h"
#include <time.h>

void conn_item::set_tim_val()
{
	gen_time = time(NULL);
	valid = true;
}

bool conn_item::check_vaild(int timeout_s)
{
	if (time(NULL) - gen_time > timeout_s)
	{
		valid = false;
	}
	return valid;
}

