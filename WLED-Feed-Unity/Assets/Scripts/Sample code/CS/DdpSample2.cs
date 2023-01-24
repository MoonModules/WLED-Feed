// DDP protocol header definitions


//C++ TO C# CONVERTER NOTE: The following #define macro was replaced in-line:
//ORIGINAL LINE: #define DDP_HEADER_LEN (sizeof(struct ddp_hdr_struct))

public class test
{
	public const int DDP_PORT = 4048;
	public const int DDP_MAX_DATALEN = 480 * 3; // fits nicely in an ethernet packet
	public const int DDP_FLAGS1_VER = 0xc0; // version mask
	public const int DDP_FLAGS1_VER1 = 0x40; // version=1
	public const int DDP_FLAGS1_PUSH = 0x01;
	public const int DDP_FLAGS1_QUERY = 0x02;
	public const int DDP_FLAGS1_REPLY = 0x04;
	public const int DDP_FLAGS1_STORAGE = 0x08;
	public const int DDP_FLAGS1_TIME = 0x10;
	public const int DDP_ID_DISPLAY = 1;
	public const int DDP_ID_CONFIG = 250;
	public const int DDP_ID_STATUS = 251;

	
	// DDP header format
	// header is 10 bytes (14 if TIME flag used)
	public struct ddp_hdr_struct
	{
		public byte flags1;
		public byte flags2;
		public byte type;
		public byte id;
		public byte offset1; // MSB
		public byte offset2;
		public byte offset3;
		public byte offset4;
		public byte len1; // MSB
		public byte len2;
	}

	private void send()
	{
		// for example code below:
		ddp_hdr_struct dh = new ddp_hdr_struct(); // header storage
		byte[] databuf; // pointer to data buffer

		int NDEVICES; // number of display devices
		int LIGHTS_PER_DEVICE; // how many RGB pairs are being sent to each display device
		byte[] RGBDATA = new byte[BUFLEN]; // the data to send

		int rgbdata_index = 0;
		for (int devnum = 0; devnum < NDEVICES; devnum++) // for each output device
		{
			int output_byte_count = 0;
			int frame_offset = 0;
			for (int i = 0; i < LIGHTS_PER_DEVICE; i++) // copy RGB values to output buffer
			{
				databuf[output_byte_count++] = RGBDATA[rgbdata_index++]; // copy R
				databuf[output_byte_count++] = RGBDATA[rgbdata_index++]; // copy G
				databuf[output_byte_count++] = RGBDATA[rgbdata_index++]; // copy B
				if (output_byte_count > (DDP_MAX_DATALEN - 3)) // if DDP packet full...
				{
					// send next DDP data packet to device
					dh.flags1 = DDP_FLAGS1_VER1;
					dh.id = DDP_ID_DISPLAY;
					dh.type = 1;
					dh.offset = frame_offset;
					dh.len = output_byte_count;
					if ((NDEVICES == 1) && (i == (LIGHTS_PER_DEVICE-1)))
					{
						dh.flags1 |= DDP_FLAGS1_PUSH; // push if only 1 device and last packet
					}
					UDP_SEND(ip_addr(devnum),DDP_PORT,dh,databuf);
					frame_offset += output_byte_count;
					output_byte_count = 0;
				}
			}
			if (output_byte_count > 0) // partial packet left to send?
			{
				// send last DDP data packet to device
				dh.flags1 = DDP_FLAGS1_VER1;
				dh.id = DDP_ID_DISPLAY;
				dh.type = 1;
				dh.offset = frame_offset;
				dh.len = output_byte_count;
				if (NDEVICES == 1)
				{
					dh.flags1 |= DDP_FLAGS1_PUSH; // push if only 1 device and last packet
				}
				UDP_SEND(ip_addr(devnum),DDP_PORT,dh,databuf);
			}
		}

		// sent data to all devices, now broadcast PUSH flag so they all display sync'd
		if (NDEVICES > 1) // if 1 device, already sent PUSH above
		{
			dh.flags1 = DDP_FLAGS1_VER1 | DDP_FLAGS1_PUSH;
			dh.id = DDP_ID_DISPLAY;
			dh.offset = 0;
			dh.len = 0;
			UDP_SEND(local - IP - broadcast - address,DDP_PORT,dh,databuf);
		}
	}
}
