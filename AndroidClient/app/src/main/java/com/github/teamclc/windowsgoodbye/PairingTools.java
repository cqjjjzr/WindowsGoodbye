package com.github.teamclc.windowsgoodbye;

import android.content.Context;
import android.os.Build;
import android.provider.Settings;
import android.util.Base64;
import android.widget.AbsListView;

import com.github.teamclc.windowsgoodbye.utils.CryptoUtils;
import com.github.teamclc.windowsgoodbye.utils.UUIDUtils;
import com.jaredrummler.android.device.DeviceName;

import java.io.ByteArrayOutputStream;
import java.io.IOError;
import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.MulticastSocket;
import java.net.SocketException;
import java.net.SocketTimeoutException;
import java.net.UnknownHostException;
import java.time.Instant;
import java.util.Arrays;
import java.util.UUID;

public class PairingTools {
    private static final int PAIRING_MULTICAST_PORT = 26817;
    private static final int PAIRING_RESULT_PORT = 26817;
    private static InetAddress PAIRING_MULTICAST_ADDR;

    static {
        try {
            PAIRING_MULTICAST_ADDR = InetAddress.getByName("225.67.76.67");
        } catch (UnknownHostException e) {
            PAIRING_MULTICAST_ADDR = null;
        }
    }

    private static final String PAIRING_PREFIX = "wingb://pair?";
    private static final String PAIRING_REQUEST_PREFIX = "wingb://pair_req?";
    private static final String PAIRING_FINISH_PREFIX = "wingb://pair_finish?";

    public static void processPairData(String qrCodeData, Context context) throws IOException {
        if (qrCodeData.length() <= PAIRING_PREFIX.length() || !qrCodeData.startsWith(PAIRING_PREFIX)) return; // TODO: throw exception
        String payload = qrCodeData.substring(PAIRING_PREFIX.length() + 1);
        byte[] payloadBytes = Base64.decode(payload, Base64.DEFAULT);

        if (payloadBytes.length != 16 + 32 + 32 + 32) return; // TODO: throw exception
        UUID deviceID = UUIDUtils.fromBytes(payloadBytes);
        byte[] deviceKey = new byte[PCInfo.KEYS_LENGTH];
        byte[] authKey = new byte[PCInfo.KEYS_LENGTH];
        byte[] pairingEncryptKey = new byte[32];

        System.arraycopy(payloadBytes, 17, deviceKey, 0, PCInfo.KEYS_LENGTH);
        System.arraycopy(payloadBytes, 49, authKey, 0, PCInfo.KEYS_LENGTH);
        System.arraycopy(payloadBytes, 81, pairingEncryptKey, 0, 32);

        // TODO: Show toast: Connecting
        requestPair(deviceID, deviceKey, authKey, pairingEncryptKey, context);
    }

    private static void requestPair(UUID deviceID, byte[] deviceKey, byte[] authKey, byte[] pairingEncryptKey, Context context) throws IOException {
        String friendlyName = Settings.Secure.getString(context.getContentResolver(), "bluetooth_name");
        String modelName = DeviceName.getDeviceName();
        byte[] friendlyNameBytes = friendlyName.substring(0, Math.max(64, friendlyName.length())).getBytes();
        byte[] modelNameBytes = modelName.substring(0, Math.max(64, modelName.length())).getBytes();
        ByteArrayOutputStream stream = new ByteArrayOutputStream(2 + friendlyNameBytes.length + modelNameBytes.length);
        stream.write(friendlyNameBytes.length);
        stream.write(modelNameBytes.length);
        stream.write(friendlyNameBytes);
        stream.write(modelNameBytes);
        byte[] encryptedData = CryptoUtils.encrypt(stream.toByteArray(), pairingEncryptKey);
        byte[] uuidBytes = UUIDUtils.toBytes(deviceID);

        byte[] payloadBytes = new byte[encryptedData.length + 16];
        System.arraycopy(uuidBytes, 0, payloadBytes, 0, uuidBytes.length);
        System.arraycopy(encryptedData, 0, payloadBytes, uuidBytes.length + 1, 0);
        String payload = PAIRING_REQUEST_PREFIX + Base64.encodeToString(payloadBytes, Base64.DEFAULT);

        byte[] data = payload.getBytes();
        MulticastSocket multicastSocket = new MulticastSocket(PAIRING_MULTICAST_PORT);
        multicastSocket.joinGroup(PAIRING_MULTICAST_ADDR);
        multicastSocket.send(new DatagramPacket(data, data.length));

        waitingForPairResponse(deviceID, deviceKey, authKey, pairingEncryptKey, context);
    }

    private static final int RECEIVE_BUFFER_SIZE = 1024;
    private static final int TIMEOUT = 30 * 1000;
    private static void waitingForPairResponse(UUID deviceID, byte[] deviceKey, byte[] authKey, byte[] pairingEncryptKey, Context context) throws IOException {
        DatagramSocket udpSocket = new DatagramSocket(PAIRING_RESULT_PORT);
        udpSocket.setSoTimeout(TIMEOUT);
        long startTimeMillis = System.currentTimeMillis();
        byte[] buf = new byte[RECEIVE_BUFFER_SIZE];
        DatagramPacket receivePacket = new DatagramPacket(buf, buf.length);
        while (true) {
            try {
                udpSocket.receive(receivePacket);
            } catch (SocketTimeoutException ex) {
                //TODO: Timeout toast
            }

            byte[] data = receivePacket.getData();

            break;
        }
    }
}
