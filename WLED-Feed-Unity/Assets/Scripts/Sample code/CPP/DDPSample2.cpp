// DDP protocol header definitions

#define DDP_PORT 4048

#define DDP_HEADER_LEN (sizeof(struct ddp_hdr_struct))
#define DDP_MAX_DATALEN (480*3)   // fits nicely in an ethernet packet

#define DDP_FLAGS1_VER     0xc0   // version mask
#define DDP_FLAGS1_VER1    0x40   // version=1
#define DDP_FLAGS1_PUSH    0x01
#define DDP_FLAGS1_QUERY   0x02
#define DDP_FLAGS1_REPLY   0x04
#define DDP_FLAGS1_STORAGE 0x08
#define DDP_FLAGS1_TIME    0x10

#define DDP_ID_DISPLAY       1
#define DDP_ID_CONFIG      250
#define DDP_ID_STATUS      251


// DDP header format
// header is 10 bytes (14 if TIME flag used)
struct ddp_hdr_struct {
    byte flags1;
    byte flags2;
    byte type;
    byte id;
    byte offset1;  // MSB
    byte offset2;
    byte offset3;
    byte offset4;
    byte len1;     // MSB
    byte len2;
};

public class DdpSample2
{
    void send()
    {
        // for example code below:
        struct ddp_hdr_struct dh;    // header storage
        unsigned char *databuf;      // pointer to data buffer

        int NDEVICES;          // number of display devices
        int LIGHTS_PER_DEVICE; // how many RGB pairs are being sent to each display device
        byte RGBDATA[BUFLEN];  // the data to send

        rgbdata_index = 0;
        for (devnum = 0; devnum < NDEVICES; devnum++)  // for each output device
            {
            output_byte_count = 0;
            frame_offset = 0;
            for (i = 0; i < LIGHTS_PER_DEVICE; i++)  // copy RGB values to output buffer
                {
                databuf[output_byte_count++] = RGBDATA[rgbdata_index++];  // copy R
                databuf[output_byte_count++] = RGBDATA[rgbdata_index++];  // copy G
                databuf[output_byte_count++] = RGBDATA[rgbdata_index++];  // copy B
                if (output_byte_count > (DDP_MAX_DATALEN-3))  // if DDP packet full...
                    {
                    // send next DDP data packet to device
                    dh.flags1 = DDP_FLAGS1_VER1;
                    dh.id     = DDP_ID_DISPLAY;
                    dh.type   = 1;
                    dh.offset = frame_offset;
                    dh.len    = output_byte_count;
                    if ((NDEVICES == 1) && (i == (LIGHTS_PER_DEVICE-1)))
                        dh.flags1 |= DDP_FLAGS1_PUSH;  // push if only 1 device and last packet
                    UDP_SEND(ip_addr(devnum),DDP_PORT,dh,databuf);         
                    frame_offset += output_byte_count;
                    output_byte_count = 0;
                    }
                }
            if (output_byte_count > 0) // partial packet left to send?
                {
                // send last DDP data packet to device
                dh.flags1 = DDP_FLAGS1_VER1;
                dh.id     = DDP_ID_DISPLAY;
                dh.type   = 1;
                dh.offset = frame_offset;
                dh.len    = output_byte_count;
                if (NDEVICES == 1)
                    dh.flags1 |= DDP_FLAGS1_PUSH;  // push if only 1 device and last packet
                UDP_SEND(ip_addr(devnum),DDP_PORT,dh,databuf);         
                }
            }

        // sent data to all devices, now broadcast PUSH flag so they all display sync'd
        if (NDEVICES > 1)  // if 1 device, already sent PUSH above
            {
            dh.flags1 = DDP_FLAGS1_VER1 | DDP_FLAGS1_PUSH;
            dh.id     = DDP_ID_DISPLAY;
            dh.offset = 0;
            dh.len    = 0;
            UDP_SEND(local-IP-broadcast-address,DDP_PORT,dh,databuf);         
            }
    }
}