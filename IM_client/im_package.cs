using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace IM
{
    public class im_package
    {
        /*      类：字段      */

        //size of this class
	    public const Int32 size_head = 100;
	    public const Int32 size_content_max = 1024;
	    //type
	    public const Int32 tp_login = 1;
	    public const Int32 tp_table = 2;
	    public const Int32 tp_text = 3;
	    public const Int32 tp_file = 4;
	    //type - costom
	    public const Int32 tp_doudizhu = 5;
	    //information
	    //type = tp_login
	    public const Int32 lg_ask = 1;
	    public const Int32 lg_answer = 2;
	    public const Int32 lg_accept = 3;
	    public const Int32 lg_reject_password = 4;
	    public const Int32 lg_reject_username = 5;
	    public const Int32 lg_reject_format = 6;
	    public const Int32 lg_logout_request = 7;
	    public const Int32 lg_logout = 8;
	    public const Int32 lg_logout_force = 9;
	    //type = tp_table
	    public const Int32 tb_all = 1;
	    public const Int32 tb_online = 2;
	    //type = tp_text
        public const Int32 tx_content = 1;
        public const Int32 tx_reply = 2;
        //type = tp_file
	    public const Int32 fl_content = 1;
	    public const Int32 fl_reply = 2;
	    public const Int32 fl_request = 3;
	    public const Int32 fl_accept = 4;
	    public const Int32 fl_reject = 5;
	    public const Int32 fl_interrupt = 6;
	    public const Int32 fl_finish = 7;
	    public const Int32 fl_offline = 8;
	    //information - costom
	    //type = tp_doudizhu
	    public const Int32 dz_online = 1;
	    public const Int32 dz_invite = 2;
	    public const Int32 dz_accept = 3;
	    public const Int32 dz_reject = 4;

        /*      对象：字段      */

	    //item
        public Int32 sender;
        public Int32 type;
        public Int32 information;
        public Int32 receiver_number;
        public Int32[] receiver;        //size = 20 * 4 (Bit)
        public Int32 content_lenth;
        public Byte[] content;          //size = size_content_max (Bit)
	    //buffer
        public Byte[] buffer;

        /*      对象：构造与析构方法      */

        //构造方法（1个参数）
        public im_package(Boolean buf)
        {
            //item
            sender = 0;
            type = 0;
            information = 0;
            receiver_number = 0;
            receiver = new Int32[20];
            content_lenth = 0;
            content = new Byte[size_content_max];
            if (buf)
            {
                buffer = new Byte[size_head + size_content_max];
            }
        }

        /*      对象：功能方法      */

        //build package
        public void build()
        {
            //sender
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(sender)).CopyTo(buffer, 0);
            //type
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(type)).CopyTo(buffer, 4);
            //information
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(information)).CopyTo(buffer, 8);
            //receiver_number
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(receiver_number)).CopyTo(buffer, 12);
            //receiver
            for (Int32 i = 0, j = 16; i < 20; ++i, j += 4)
            {
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(receiver[i])).CopyTo(buffer, j);
            }
            //content_lenth
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(content_lenth)).CopyTo(buffer, 96);
            //content
            Array.Copy(content, 0, buffer, 100, content_lenth);
        }

        //split package
        public void split()
        {
            //sender
            sender = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
            //type
            type = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 4));
            //information
            information = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 8));
            //receiver_number
            receiver_number = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 12));
            //receiver
            for (Int32 i = 0, j = 16; i < 20; ++i, j += 4)
            {
                receiver[i] = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, j));
            }
            //content_lenth
            content_lenth = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 96));
            //content
            Array.Copy(buffer, 100, content, 0, content_lenth);
        }

        //preread lenth of content
        public Int32 preread()
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 96));
        }
    }
}
