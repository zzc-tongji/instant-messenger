exec   = IM_server
source = my_daemon.c im_package.cpp conn_item.cpp auth_item.cpp IM_server.cpp
head   = my_daemon.h im_package.h   conn_item.h   auth_item.h   compiler_option.h

$(exec) : $(source) $(head)
	g++ $(source) -o $(exec)

.PHONY : clean
clean :
	rm -f $(exec)
