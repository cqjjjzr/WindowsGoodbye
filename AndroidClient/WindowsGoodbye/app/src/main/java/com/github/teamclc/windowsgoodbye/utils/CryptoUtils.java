package com.github.teamclc.windowsgoodbye.utils;

import java.security.InvalidAlgorithmParameterException;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;

import javax.crypto.BadPaddingException;
import javax.crypto.Cipher;
import javax.crypto.IllegalBlockSizeException;
import javax.crypto.NoSuchPaddingException;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;

public class CryptoUtils {
    public static final byte[] IV = { 0x43, 0x79, 0x43, 0x68, 0x61, 0x72, 0x6c, 0x69, 0x65, 0x4c, 0x61, 0x73, 0x6d, 0x43, 0x4c, 0x43 };

    public static byte[] encrypt(byte[] data, byte[] key) {
        return processAES(data, key, Cipher.ENCRYPT_MODE);
    }

    public static byte[] decrypt(byte[] data, byte[] key) {
        return processAES(data, key, Cipher.DECRYPT_MODE);
    }

    private static byte[] processAES(byte[] data, byte[] key, int mode) {
        Cipher cipher;
        try {
            cipher = Cipher.getInstance("AES/CBC/PKCS5Padding");
            cipher.init(mode, new SecretKeySpec(key, "AES"), new IvParameterSpec(IV));
        } catch (NoSuchAlgorithmException |
                NoSuchPaddingException |
                InvalidAlgorithmParameterException |
                InvalidKeyException e) { return null; } // SHOULD NEVER HAPPEN
        try {
            return cipher.doFinal(data);
        } catch (IllegalBlockSizeException | BadPaddingException e) { /* IGNORED */ }
        return null;
    }
}
