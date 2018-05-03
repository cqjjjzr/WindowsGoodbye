package com.github.teamclc.windowsgoodbye;

import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.util.Base64;
import android.widget.Toast;

import com.github.teamclc.windowsgoodbye.model.PCInfo;
import com.mtramin.rxfingerprint.EncryptionMethod;
import com.mtramin.rxfingerprint.RxFingerprint;
import com.mtramin.rxfingerprint.data.FingerprintEncryptionResult;

import io.reactivex.disposables.Disposable;
import io.reactivex.functions.Consumer;

public class FingerprintManager {
    private static AlertDialog createAskForEncryptFingerprintDialog(Context context) {
        AlertDialog.Builder builder;
        builder = new AlertDialog.Builder(context, android.R.style.Theme_Material_Dialog_Alert);
        return builder.setTitle("Delete entry")
                .setMessage(R.string.use_fingerprint_to_pair)
                .setNegativeButton(android.R.string.cancel, new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int which) {
                        // give up
                    }
                })
                .setIcon(R.drawable.ic_fingerprint)
                .create();
    }

    public static void encryptByFingerprint(final Context context, PCInfo info) {
        AlertDialog dialog = createAskForEncryptFingerprintDialog(context);

        String str =
                Base64.encodeToString(info.getDeviceKey(), Base64.DEFAULT)
                        + " "
                        + Base64.encodeToString(info.getAuthKey(), Base64.DEFAULT);
        final Disposable disposable = RxFingerprint.encrypt(EncryptionMethod.AES, context, info.getDeviceID().toString(), str)
                .subscribe(new Consumer<FingerprintEncryptionResult>() {
                    @Override
                    public void accept(FingerprintEncryptionResult fingerprintEncryptionResult) throws Exception {
                        switch (fingerprintEncryptionResult.getResult()) {
                            case FAILED:
                                Toast.makeText(context, "Fingerprint not recognized, try again!", Toast.LENGTH_LONG).show();
                                break;
                            case HELP:
                                Toast.makeText(context, fingerprintEncryptionResult.getMessage(), Toast.LENGTH_LONG).show();
                                break;
                            case AUTHENTICATED:
                                fingerprintEncryptionResult.getEncrypted();
                                Toast.makeText(context, "Successfully authenticated!", Toast.LENGTH_LONG).show();
                                break;
                        }
                    }
                }, new Consumer<Throwable>() {
                    @Override
                    public void accept(Throwable throwable) throws Exception {

                    }
                });

        dialog.setButton(DialogInterface.BUTTON_NEGATIVE, context.getText(android.R.string.cancel), new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                disposable.dispose();
                dialog.cancel();
            }
        });
        dialog.show();
    }
}
