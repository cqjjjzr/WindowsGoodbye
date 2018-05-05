package com.github.teamclc.windowsgoodbye.utils;

import android.util.Log;

import java.nio.ByteBuffer;
import java.util.UUID;

public class UUIDUtils {
    public static UUID fromBytes(byte[] bytes) {
        return fromBytes(bytes, 0);
    }

    public static UUID fromBytes(byte[] bytes, int off) {
        if (bytes.length < off + 16) throw new IllegalArgumentException("length < 16");
        ByteBuffer bb = ByteBuffer.wrap(bytes);
        long firstLong = bb.getLong();
        long secondLong = bb.getLong();
        Log.v("uuid", "uuid converting: msb=" + firstLong + ", lsb=" + secondLong);
        return new UUID(firstLong, secondLong);
    }

    public static byte[] toBytes(UUID uuid) {
        ByteBuffer bb = ByteBuffer.wrap(new byte[16]);
        bb.putLong(uuid.getMostSignificantBits());
        bb.putLong(uuid.getLeastSignificantBits());
        return bb.array();
    }
}
