package com.github.teamclc.windowsgoodbye;

import android.content.Context;
import android.util.Log;

import java.net.InetAddress;

public class IncomingPacketProcessor {
    public static final String PAIRING_TERMINATE = "wingb://pair_terminate";
    public static final String PAIRING_FINISHED = "wingb://pair_finish?";

    public static void processMulticast(byte[] content, int len, InetAddress from, Context ctxt) {
        try {
            String str = new String(content, 0, len);
            if (str.startsWith(PAIRING_TERMINATE)) {
                Log.e("incomingMulticast", "received terminate pkt! ");
                PairingTools.terminateCurrentPairing(ctxt);
            }
        } catch (Exception ex) {
            Log.e("incomingMulticast", "Error processing incoming pkt!", ex);
        }
    }

    public static void processUnicast(byte[] content, int len, InetAddress from, Context ctxt) {
        try {
            String str = new String(content, 0, len);
            if (str.startsWith(PAIRING_FINISHED) && str.length() > PAIRING_FINISHED.length()) {
                String payload = str.substring(PAIRING_FINISHED.length());
                Log.e("incomingMulticast", "received pairing finish pkt! " + payload);
                PairingTools.finishPairing(payload, ctxt);
            }
        } catch (Exception ex) {
            Log.e("incomingUnicast", "Error processing incoming pkt!", ex);
        }
    }
}
