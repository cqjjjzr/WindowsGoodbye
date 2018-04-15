package com.github.teamclc.windowsgoodbye;

import android.content.Context;
import android.os.Build;
import android.provider.Settings;
import android.util.Base64;

import com.github.teamclc.windowsgoodbye.utils.UUIDUtils;
import com.jaredrummler.android.device.DeviceName;

import java.io.IOError;
import java.io.IOException;
import java.net.DatagramSocket;
import java.net.MulticastSocket;
import java.net.SocketException;
import java.util.Arrays;
import java.util.UUID;

public class PairingTools {
    private static final int PAIRING_MULTICAST_PORT = 26817;

    private static final String PAIRING_PREFIX = "wingb://pair?";

    public static void processPairData(String qrCodeData, Context context) throws IOException {
        if (qrCodeData.length() <= PAIRING_PREFIX.length() || !qrCodeData.startsWith(PAIRING_PREFIX)) return;
        String payload = qrCodeData.substring(PAIRING_PREFIX.length() + 1);
        byte[] bytes = Base64.decode(payload, Base64.DEFAULT);

        UUID deviceID = UUIDUtils.fromBytes(bytes);
        byte[] deviceKey = new byte[PCInfo.KEYS_LENGTH];
        byte[] authKey = new byte[PCInfo.KEYS_LENGTH];

        System.arraycopy(bytes, 17, deviceKey, 0, 32);
        System.arraycopy(bytes, 49, authKey, 0, 32);

        requestPair(deviceKey, authKey, context);
    }

    private static void requestPair(byte[] deviceKey, byte[] authKey, Context context) throws IOException {
        String friendlyName = Settings.Secure.getString(context.getContentResolver(), "bluetooth_name");
        String modelName = DeviceName.getDeviceName();

        MulticastSocket multicastSocket = new MulticastSocket(PAIRING_MULTICAST_PORT);
    }
}
