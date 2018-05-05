package com.github.teamclc.windowsgoodbye;

import android.app.Activity;
import android.bluetooth.BluetoothAdapter;
import android.content.Context;
import android.os.AsyncTask;
import android.os.Handler;
import android.os.Looper;
import android.util.Base64;
import android.util.Log;
import android.widget.Toast;

import com.github.teamclc.windowsgoodbye.db.DbHelper;
import com.github.teamclc.windowsgoodbye.model.PCInfo;
import com.github.teamclc.windowsgoodbye.utils.CryptoUtils;
import com.github.teamclc.windowsgoodbye.utils.UUIDUtils;
import com.jaredrummler.android.device.DeviceName;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.net.DatagramPacket;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.util.UUID;

import io.reactivex.functions.Consumer;

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

    private static byte[] activeEncryptionKey;


    public static void processPairData(String qrCodeData, final Activity context) throws IOException {
        if (qrCodeData.length() <= PAIRING_PREFIX.length() || !qrCodeData.startsWith(PAIRING_PREFIX)) return; // TODO: throw exception
        String payload = qrCodeData.substring(PAIRING_PREFIX.length());
        Log.i("PairingTools", "Payload, " + payload);
        byte[] payloadBytes = Base64.decode(payload, Base64.DEFAULT);

        Log.i("PairingTools", "Starting, " + payloadBytes.length);
        if (payloadBytes.length != 16 + 32 + 32 + 32) return; // TODO: throw exception
        UUID deviceID = UUIDUtils.fromBytes(payloadBytes);
        byte[] deviceKey = new byte[PCInfo.KEYS_LENGTH];
        byte[] authKey = new byte[PCInfo.KEYS_LENGTH];
        final byte[] pairingEncryptKey = new byte[32];

        System.arraycopy(payloadBytes, 16, deviceKey, 0, PCInfo.KEYS_LENGTH);
        System.arraycopy(payloadBytes, 48, authKey, 0, PCInfo.KEYS_LENGTH);
        System.arraycopy(payloadBytes, 80, pairingEncryptKey, 0, 32);

        // TODO: Show toast: Connecting
        Log.i("PairingTools", "Connecting");

        final PCInfo pcInfo = new PCInfo(deviceID, deviceKey, authKey);
        deviceKey = null;
        authKey = null;
        context.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                FingerprintManager.encryptByFingerprint(context, pcInfo, new Consumer<String>() {
                    @Override
                    public void accept(String s) {
                        pcInfo.setAuthKey(null);
                        pcInfo.setDeviceKey(null);
                        pcInfo.setEncryptedKeys(s);
                        System.gc(); // Delete unencrypted dk and ak in memory
                        new RequestPairTask().execute(context, pairingEncryptKey, pcInfo);
                    }
                });
            }
        });
    }

    private static class RequestPairTask extends AsyncTask<Object, String, Void> {
        private Context context;

        @Override
        protected Void doInBackground(Object... objects) {
            try {
                context = (Context) objects[0];
                byte[] pairingEncryptKey = (byte[]) objects[1];
                PCInfo pcInfo = (PCInfo) objects[2];

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
                new DbHelper(context).addPCInfo(pcInfo);
                activeEncryptionKey = pairingEncryptKey;
                UDPListeningService.allocNewMulticastSocket().send(new DatagramPacket(data, data.length, UDPListeningService.MULTICAST_GROUP, UDPListeningService.MULTICAST_PORT));
            } catch (Exception ex) {
                publishProgress(ex.getClass().getSimpleName() + ": " + ex.getLocalizedMessage());
            }
            return null;
        }

        @Override
        protected void onProgressUpdate(String... values) {
            Toast.makeText(context, context.getString(R.string.pair_failed_exception, values[0]), Toast.LENGTH_LONG).show();
        }
    }

    // Called in a service
    public static void terminateCurrentPairing(final Context context) {
        new Handler(Looper.getMainLooper()).post(new Runnable() {
            @Override
            public void run() {
                Toast.makeText(context, R.string.pair_user_canceled, Toast.LENGTH_LONG).show();
            }
        });
    }

    // Called in a service
    public static void terminateCurrentPairingAlsoOnPC(Context context) {
        try {
            // TODO send terminate packet
        } catch (Exception ex) {
            // IGNORED
        }
        terminateCurrentPairing(context);
    }

    // Called in a service
    public static void finishPairing(String payload, final Context context) {
        try {
            byte[] bytes = Base64.decode(payload, Base64.DEFAULT);
            if (activeEncryptionKey == null) {
                Log.e("pairing", "pek is null???");
                return;
            }
            final String name = new String(CryptoUtils.decrypt(bytes, activeEncryptionKey));
            new DbHelper(context).finishActivePairing(name);
            new Handler(Looper.getMainLooper()).post(new Runnable() {
                @Override
                public void run() {
                    Toast.makeText(context, context.getString(R.string.pair_successful, name), Toast.LENGTH_LONG).show();
                }
            });
        } catch (final Exception ex) {
            new Handler(Looper.getMainLooper()).post(new Runnable() {
                @Override
                public void run() {
                    Toast.makeText(context, context.getString(R.string.pair_failed_exception, ex.getClass().getSimpleName() + ": " + ex), Toast.LENGTH_LONG).show();
                }
            });
        }
    }
}
