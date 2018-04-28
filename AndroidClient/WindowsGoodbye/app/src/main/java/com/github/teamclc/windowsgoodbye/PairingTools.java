package com.github.teamclc.windowsgoodbye;

import android.bluetooth.BluetoothAdapter;
import android.content.Context;
import android.util.Base64;
import android.util.Log;

import com.github.teamclc.windowsgoodbye.utils.CryptoUtils;
import com.github.teamclc.windowsgoodbye.utils.UUIDUtils;
import com.jaredrummler.android.device.DeviceName;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.net.DatagramPacket;
import java.net.InetAddress;
import java.net.UnknownHostException;
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
        String payload = qrCodeData.substring(PAIRING_PREFIX.length());
        Log.i("PairingTools", "Payload, " + payload);
        byte[] payloadBytes = Base64.decode(payload, Base64.DEFAULT);

        Log.i("PairingTools", "Starting, " + payloadBytes.length);
        if (payloadBytes.length != 16 + 32 + 32 + 32) return; // TODO: throw exception
        UUID deviceID = UUIDUtils.fromBytes(payloadBytes);
        byte[] deviceKey = new byte[PCInfo.KEYS_LENGTH];
        byte[] authKey = new byte[PCInfo.KEYS_LENGTH];
        byte[] pairingEncryptKey = new byte[32];

        System.arraycopy(payloadBytes, 16, deviceKey, 0, PCInfo.KEYS_LENGTH);
        System.arraycopy(payloadBytes, 48, authKey, 0, PCInfo.KEYS_LENGTH);
        System.arraycopy(payloadBytes, 80, pairingEncryptKey, 0, 32);

        // TODO: Show toast: Connecting
        Log.i("PairingTools", "Connecting");

        PCInfo pcInfo = new PCInfo(deviceID, deviceKey, authKey);
        requestPair(pcInfo, pairingEncryptKey, context);
    }

    private static void requestPair(PCInfo pcInfo, byte[] pairingEncryptKey, Context context) throws IOException {
        String friendlyName = BluetoothAdapter.getDefaultAdapter().getName();
        String modelName = DeviceName.getDeviceName();
        Log.i("PairingTools", "friendly name " + friendlyName + ", model name " + modelName);
        byte[] friendlyNameBytes = friendlyName.substring(0, Math.min(64, friendlyName.length())).getBytes();
        byte[] modelNameBytes = modelName.substring(0, Math.min(64, modelName.length())).getBytes();
        ByteArrayOutputStream stream = new ByteArrayOutputStream(2 + friendlyNameBytes.length + modelNameBytes.length);
        stream.write(friendlyNameBytes.length);
        stream.write(modelNameBytes.length);
        stream.write(friendlyNameBytes);
        stream.write(modelNameBytes);
        byte[] encryptedData = CryptoUtils.encrypt(stream.toByteArray(), pairingEncryptKey);
        byte[] uuidBytes = UUIDUtils.toBytes(pcInfo.getDeviceID());

        byte[] payloadBytes = new byte[encryptedData.length + 16];
        System.arraycopy(uuidBytes, 0, payloadBytes, 0, uuidBytes.length);
        System.arraycopy(encryptedData, 0, payloadBytes, uuidBytes.length, encryptedData.length);
        String payload = PAIRING_REQUEST_PREFIX + Base64.encodeToString(payloadBytes, Base64.DEFAULT);

        byte[] data = payload.getBytes();
        MulticastListeningService.allocNewSocket().send(new DatagramPacket(data, data.length, MulticastListeningService.MULTICAST_GROUP, MulticastListeningService.PORT));

        // TODO: Add pc to persist
    }
}
