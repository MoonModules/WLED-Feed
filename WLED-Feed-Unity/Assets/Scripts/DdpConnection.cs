using System;
using System.Net;
using UnityEngine;

namespace DefaultNamespace
{
    public class DdpConnection : UdpConnection
    {
        private const int DDP_DEFAULT_PORT = 1448;
        private const int DDP_CHANNELS_PER_PACKET = 1440; // 480 leds
        private const int DDP_TYPE_RGB24 = 0x0A;
        private const int DDP_TYPE_RGBW32 = 0x1A;
        private const int DDP_ID_DISPLAY = 1;
        private const byte DDP_FLAGS1_PUSH = 0x01;
        private const byte DDP_FLAGS1_VER1 = 0x40; // version=1

        //
        // Send real time UDP updates to the specified client
        //
        // type   - protocol type (0=DDP, 1=E1.31, 2=ArtNet)
        // client - the IP address to send to
        // length - the number of pixels
        // buffer - a buffer of at least length*4 bytes long
        // isRGBW - true if the buffer contains 4 components per pixel

        int sequenceNumber = 0; // this needs to be shared across all outputs

        string tx_buffer = ""; //string of bytes

        bool beginPacket(string ip, int port) {

            tx_buffer = "";

            //init udp_server socket if not done already
            //StartConnection(ip, port, port); //sendPort, int receivePort??

            return true; //0 if something went wrong in udp server
        }

        bool endPacket(){
            Send(tx_buffer);
            return true; //If Send has an error, return 0
        }

        bool write(int data){
            return write((byte)data);
        }

        bool write(byte data){
            if(tx_buffer.Length == 1460) {
                endPacket();
            }
            tx_buffer += data; //add to string o 
            return true;
        }


        byte realtimeBroadcast(byte type, string client, int length, byte[] buffer, byte bri, bool isRGBW)
        {
            if (client=="" || length==0) return 1; // network not initialised or dummy/unset IP address  031522 ajn added check for ap

            switch (type)
            {
                case 0: // DDP
                    // calculate the number of UDP packets we need to send
                    int channelCount = length * (isRGBW ? 4 : 3); // 1 channel for every R,G,B value
                    int packetCount = ((channelCount - 1) / DDP_CHANNELS_PER_PACKET) + 1;

                    // there are 3 channels per RGB pixel
                    int channel = 0;
                    // the current position in the buffer
                    int bufferOffset = 0;

                    for (int currentPacket = 0; currentPacket < packetCount; currentPacket++)
                    {
                        if (sequenceNumber > 15) sequenceNumber = 0;

                        if (beginPacket(client, DDP_DEFAULT_PORT)) {  // port defined in ESPAsyncE131.h
                            // port defined in ESPAsyncE131.h
                            Debug.LogError("WiFiUDP.beginPacket returned an error");
                            return 1; // problem
                        }

                        // the amount of data is AFTER the header in the current packet
                        int packetSize = DDP_CHANNELS_PER_PACKET;

                        byte flags = DDP_FLAGS1_VER1;
                        if (currentPacket == (packetCount - 1U))
                        {
                            // last packet, set the push flag
                            // TODO: determine if we want to send an empty push packet to each destination after sending the pixel data
                            flags = DDP_FLAGS1_VER1 | DDP_FLAGS1_PUSH;
                            if (channelCount % DDP_CHANNELS_PER_PACKET == 0)
                            {
                                packetSize = channelCount % DDP_CHANNELS_PER_PACKET;
                            }
                        }

                        // write the header
                        /*0*/
                        write(flags); 
                        /*1*/
                        write(sequenceNumber++ & 0x0F); // sequence may be unnecessary unless we are sending twice (as requested in Sync settings)
                        /*2*/
                        write(isRGBW ? DDP_TYPE_RGBW32 : DDP_TYPE_RGB24);
                        /*3*/
                        write(DDP_ID_DISPLAY);
                        // data offset in bytes, 32-bit number, MSB first
                        /*4*/
                        write(0xFF & (channel >> 24));
                        /*5*/
                        write(0xFF & (channel >> 16));
                        /*6*/
                        write(0xFF & (channel >> 8));
                        /*7*/
                        write(0xFF & (channel));
                        // data length in bytes, 16-bit number, MSB first
                        /*8*/
                        write(0xFF & (packetSize >> 8));
                        /*9*/
                        write(0xFF & (packetSize));

                        // write the colors, the write write(const byte *buffer, int size)
                        // function is just a loop internally too
                        for (int i = 0; i < packetSize; i += 3)
                        {
                            write(scale8(buffer[bufferOffset++], bri)); // R
                            write(scale8(buffer[bufferOffset++], bri)); // G
                            write(scale8(buffer[bufferOffset++], bri)); // B
                            if (isRGBW) write(scale8(buffer[bufferOffset++], bri)); // W
                        }

                        if (!endPacket())
                        {
                            Debug.LogError("WiFiUDP.endPacket returned an error");
                            return 1; // problem
                        }

                        channel += packetSize;
                    } // for

                    break;

                case 1: //E1.31
                    break;

                case 2: //ArtNet
                    break;
            } // switch

            return 0;
        }

        private byte scale8( byte i, byte scale) {
            return (byte)((i * (1+scale)) >> 8);
        }

    }
}