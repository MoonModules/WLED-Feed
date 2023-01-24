uint8_t realtimeBroadcast(uint8_t type, IPAddress client, uint16_t length, uint8_t *buffer, uint8_t bri, bool isRGBW)  {
  if (!(apActive || interfacesInited) || !client[0] || !length) return 1;  // network not initialised or dummy/unset IP address  031522 ajn added check for ap

  WiFiUDP ddpUdp;

  switch (type) {
    case 0: // DDP
    {
      // calculate the number of UDP packets we need to send
      size_t channelCount = length * (isRGBW? 4:3); // 1 channel for every R,G,B value
      size_t packetCount = ((channelCount-1) / DDP_CHANNELS_PER_PACKET) +1;

      // there are 3 channels per RGB pixel
      uint32_t channel = 0; // TODO: allow specifying the start channel
      // the current position in the buffer
      size_t bufferOffset = 0;

      for (size_t currentPacket = 0; currentPacket < packetCount; currentPacket++) {
        if (sequenceNumber > 15) sequenceNumber = 0;

        if (!ddpUdp.beginPacket(client, DDP_DEFAULT_PORT)) {  // port defined in ESPAsyncE131.h
          DEBUG_PRINTLN(F("WiFiUDP.beginPacket returned an error"));
          return 1; // problem
        }

        // the amount of data is AFTER the header in the current packet
        size_t packetSize = DDP_CHANNELS_PER_PACKET;

        uint8_t flags = DDP_FLAGS1_VER1;
        if (currentPacket == (packetCount - 1U)) {
          // last packet, set the push flag
          // TODO: determine if we want to send an empty push packet to each destination after sending the pixel data
          flags = DDP_FLAGS1_VER1 | DDP_FLAGS1_PUSH;
          if (channelCount % DDP_CHANNELS_PER_PACKET) {
            packetSize = channelCount % DDP_CHANNELS_PER_PACKET;
          }
        }

        // write the header
        /*0*/ddpUdp.write(flags);
        /*1*/ddpUdp.write(sequenceNumber++ & 0x0F); // sequence may be unnecessary unless we are sending twice (as requested in Sync settings)
        /*2*/ddpUdp.write(isRGBW ?  DDP_TYPE_RGBW32 : DDP_TYPE_RGB24);
        /*3*/ddpUdp.write(DDP_ID_DISPLAY);
        // data offset in bytes, 32-bit number, MSB first
        /*4*/ddpUdp.write(0xFF & (channel >> 24));
        /*5*/ddpUdp.write(0xFF & (channel >> 16));
        /*6*/ddpUdp.write(0xFF & (channel >>  8));
        /*7*/ddpUdp.write(0xFF & (channel      ));
        // data length in bytes, 16-bit number, MSB first
        /*8*/ddpUdp.write(0xFF & (packetSize >> 8));
        /*9*/ddpUdp.write(0xFF & (packetSize     ));

        // write the colors, the write write(const uint8_t *buffer, size_t size)
        // function is just a loop internally too
        for (size_t i = 0; i < packetSize; i += 3) {
          ddpUdp.write(scale8(buffer[bufferOffset++], bri)); // R
          ddpUdp.write(scale8(buffer[bufferOffset++], bri)); // G
          ddpUdp.write(scale8(buffer[bufferOffset++], bri)); // B
          if (isRGBW) ddpUdp.write(scale8(buffer[bufferOffset++], bri)); // W
        }

        if (!ddpUdp.endPacket()) {
          DEBUG_PRINTLN(F("WiFiUDP.endPacket returned an error"));
          return 1; // problem
        }

        channel += packetSize;
      }
    } break;

    case 1: //E1.31
    {
    } break;

    case 2: //ArtNet
    {
    } break;
  }
  return 0;
}