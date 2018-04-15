package com.github.teamclc.windowsgoodbye.utils;

import java.util.UUID;

public class UUIDUtils {
    public static UUID fromBytes(byte[] bytes) {
        return fromBytes(bytes, 0);
    }

    public static UUID fromBytes(byte[] bytes, int off) {
        if (bytes.length < off + 16) throw new IllegalArgumentException("length < 16");
        long msb = 0;
        long lsb = 0;
        for (int i = 0; i < 8; i++)
            msb = (msb << 8) | (bytes[i] & 0xff);
        for (int i = 8; i < 16; i++)
            lsb = (lsb << 8) | (bytes[i] & 0xff);

        return new UUID(msb, lsb);
    }
}
