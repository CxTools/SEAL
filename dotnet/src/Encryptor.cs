﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Research.SEAL.Tools;
using System;
using System.IO;

namespace Microsoft.Research.SEAL
{
    /// <summary>
    /// Encrypts Plaintext objects into Ciphertext objects. Constructing an Encryptor requires
    /// a SEALContext with valid encryption parameters, and the public key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Overloads
    /// For the encrypt function we provide two overloads concerning the memory pool used in
    /// allocations needed during the operation. In one overload the global memory pool is used
    /// for this purpose, and in another overload the user can supply a MemoryPoolHandle
    /// to to be used instead. This is to allow one single Encryptor to be used concurrently by
    /// several threads without running into thread contention in allocations taking place during
    /// operations. For example, one can share one single Encryptor across any number of threads,
    /// but in each thread call the encrypt function by giving it a thread-local MemoryPoolHandle
    /// to use. It is important for a developer to understand how this works to avoid unnecessary
    /// performance bottlenecks.
    /// </para>
    /// <para>
    /// NTT form
    /// When using the BFV scheme (SchemeType.BFV), all plaintext and ciphertexts should
    /// remain by default in the usual coefficient representation, i.e. not in NTT form.
    /// When using the CKKS scheme (SchemeType.CKKS), all plaintexts and ciphertexts
    /// should remain by default in NTT form. We call these scheme-specific NTT states the
    /// "default NTT form". Decryption requires the input ciphertexts to be in the default
    /// NTT form, and will throw an exception if this is not the case.
    /// </para>
    /// </remarks>
    public class Encryptor : NativeObject
    {
        /// <summary>
        /// Creates an Encryptor instance initialized with the specified SEALContext,
        /// public key, and/or secret key.
        /// </summary>
        /// <param name="context">The SEALContext</param>
        /// <param name="publicKey">The public key</param>
        /// <param name="secretKey">The secret key</param>
        /// <exception cref="ArgumentNullException">if context is null</exception>
        /// <exception cref="ArgumentException">if the context is not set or encryption
        /// parameters are not valid</exception>
        /// <exception cref="ArgumentException">if publicKey is not valid</exception>
        /// <exception cref="ArgumentException">if secretKey is not null and not valid</exception>
        public Encryptor(SEALContext context, PublicKey publicKey, SecretKey secretKey = null)
        {
            if (null == context)
                throw new ArgumentNullException(nameof(context));
            if (!context.ParametersSet)
                throw new ArgumentException("Encryption parameters are not set correctly");
            if (!ValCheck.IsValidFor(publicKey, context))
                throw new ArgumentException("Public key is not valid for encryption parameters");

            if (null == secretKey)
            {
                NativeMethods.Encryptor_Create(context.NativePtr, publicKey.NativePtr, IntPtr.Zero, out IntPtr ptr);
                NativePtr = ptr;
            }
            else
            {
                if (!ValCheck.IsValidFor(secretKey, context))
                    throw new ArgumentException("Secret key is not valid for encryption parameters");
                NativeMethods.Encryptor_Create(context.NativePtr, publicKey.NativePtr, secretKey.NativePtr, out IntPtr ptr);
                NativePtr = ptr;
            }
        }

        /// <summary>
        /// Creates an Encryptor instance initialized with the specified SEALContext
        /// and secret key.
        /// </summary>
        /// <param name="context">The SEALContext</param>
        /// <param name="secretKey">The secret key</param>
        /// <exception cref="ArgumentNullException">if context is null</exception>
        /// <exception cref="ArgumentException">if the context is not set or encryption
        /// parameters are not valid</exception>
        /// <exception cref="ArgumentException">if secretKey is not valid</exception>
        public Encryptor(SEALContext context, SecretKey secretKey)
        {
            if (null == context)
                throw new ArgumentNullException(nameof(context));
            if (!context.ParametersSet)
                throw new ArgumentException("Encryption parameters are not set correctly");
            if (!ValCheck.IsValidFor(secretKey, context))
                throw new ArgumentException("Secret key is not valid for encryption parameters");

            NativeMethods.Encryptor_Create(context.NativePtr, IntPtr.Zero, secretKey.NativePtr, out IntPtr ptr);
            NativePtr = ptr;
        }

        /// <summary>
        /// Give a new instance of public key.
        /// </summary>
        /// <param name="publicKey">The public key</param>
        /// <exception cref="ArgumentException">if publicKey is not valid</exception>
        public void SetPublicKey(PublicKey publicKey)
        {
            NativeMethods.Encryptor_SetPublicKey(NativePtr, publicKey.NativePtr);
        }

        /// <summary>
        /// Give a new instance of secret key.
        /// </summary>
        /// <param name="secretKey">The secret key</param>
        /// <exception cref="ArgumentException">if secretKey is not valid</exception>
        public void SetSecretKey(SecretKey secretKey)
        {
            NativeMethods.Encryptor_SetSecretKey(NativePtr, secretKey.NativePtr);
        }

        /// <summary>
        /// Encrypts a plaintext with the public key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to:
        /// 1) in BFV, the highest (data) level in the modulus switching chain,
        /// 2) in CKKS, the encryption parameters of the plaintext.
        /// Dynamic memory allocations in the process are allocated from the memory
        /// pool pointed to by the given MemoryPoolHandle.
        /// </remarks>
        /// <param name="plain">The plaintext to encrypt</param>
        /// <param name="destination">The ciphertext to overwrite with the encrypted plaintext</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if either plain or destination are null</exception>
        /// <exception cref="ArgumentException">if plain is not valid for the encryption parameters</exception>
        /// <exception cref="ArgumentException">if plain is not in default NTT form</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public void Encrypt(Plaintext plain, Ciphertext destination, MemoryPoolHandle pool = null)
        {
            if (null == plain)
                throw new ArgumentNullException(nameof(plain));
            if (null == destination)
                throw new ArgumentNullException(nameof(destination));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            NativeMethods.Encryptor_Encrypt(NativePtr, plain.NativePtr, destination.NativePtr, poolHandle);
        }

        /// <summary>
        /// Encrypts a zero plaintext with the public key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to the given
        /// parms_id. Dynamic memory allocations in the process are allocated from the memory
        /// pool pointed to by the given MemoryPoolHandle.
        /// </remarks>
        /// <param name="parmsId">The ParmsId for the resulting ciphertext</param>
        /// <param name="destination">The ciphertext to overwrite with the encrypted plaintext</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if either parmsId or destination are null</exception>
        /// <exception cref="ArgumentException">if parmsId is not valid for the encryption parameters</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public void EncryptZero(ParmsId parmsId, Ciphertext destination, MemoryPoolHandle pool = null)
        {
            if (null == parmsId)
                throw new ArgumentNullException(nameof(parmsId));
            if (null == destination)
                throw new ArgumentNullException(nameof(destination));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            NativeMethods.Encryptor_EncryptZero1(NativePtr, parmsId.Block, destination.NativePtr, poolHandle);
        }

        /// <summary>
        /// Encrypts a zero plaintext with the public key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to the
        /// highest (data) level in the modulus switching chain. Dynamic memory allocations in
        /// the process are allocated from the memory pool pointed to by the given MemoryPoolHandle.
        /// </remarks>
        /// <param name="destination">The ciphertext to overwrite with the encrypted plaintext</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if destination is null</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public void EncryptZero(Ciphertext destination, MemoryPoolHandle pool = null)
        {
            if (null == destination)
                throw new ArgumentNullException(nameof(destination));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            NativeMethods.Encryptor_EncryptZero2(NativePtr, destination.NativePtr, poolHandle);
        }

        /// <summary>
        /// Encrypts a plaintext with the secret key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to:
        /// 1) in BFV, the highest (data) level in the modulus switching chain,
        /// 2) in CKKS, the encryption parameters of the plaintext.
        /// Dynamic memory allocations in the process are allocated from the memory
        /// pool pointed to by the given MemoryPoolHandle.
        /// </remarks>
        /// <param name="plain">The plaintext to encrypt</param>
        /// <param name="destination">The ciphertext to overwrite with the encrypted plaintext</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if either plain or destination are null</exception>
        /// <exception cref="ArgumentException">if plain is not valid for the encryption parameters</exception>
        /// <exception cref="ArgumentException">if plain is not in default NTT form</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public void EncryptSymmetric(Plaintext plain, Ciphertext destination, MemoryPoolHandle pool = null)
        {
            if (null == plain)
                throw new ArgumentNullException(nameof(plain));
            if (null == destination)
                throw new ArgumentNullException(nameof(destination));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            NativeMethods.Encryptor_EncryptSymmetric(NativePtr, plain.NativePtr, false, destination.NativePtr, poolHandle);
        }

        /// <summary>
        /// Encrypts a zero plaintext with the secret key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to the given
        /// parms_id. Dynamic memory allocations in the process are allocated from the memory
        /// pool pointed to by the given MemoryPoolHandle.
        /// </remarks>
        /// <param name="parmsId">The ParmsId for the resulting ciphertext</param>
        /// <param name="destination">The ciphertext to overwrite with the encrypted plaintext</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if either parmsId or destination are null</exception>
        /// <exception cref="ArgumentException">if parmsId is not valid for the encryption parameters</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public void EncryptZeroSymmetric(ParmsId parmsId, Ciphertext destination, MemoryPoolHandle pool = null)
        {
            if (null == parmsId)
                throw new ArgumentNullException(nameof(parmsId));
            if (null == destination)
                throw new ArgumentNullException(nameof(destination));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            NativeMethods.Encryptor_EncryptZeroSymmetric1(NativePtr, parmsId.Block, false, destination.NativePtr, poolHandle);
        }

        /// <summary>
        /// Encrypts a zero plaintext with the secret key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to the
        /// highest (data) level in the modulus switching chain. Dynamic memory allocations in
        /// the process are allocated from the memory pool pointed to by the given MemoryPoolHandle.
        /// </remarks>
        /// <param name="destination">The ciphertext to overwrite with the encrypted plaintext</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if destination is null</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public void EncryptZeroSymmetric(Ciphertext destination, MemoryPoolHandle pool = null)
        {
            if (null == destination)
                throw new ArgumentNullException(nameof(destination));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            NativeMethods.Encryptor_EncryptZeroSymmetric2(NativePtr, false, destination.NativePtr, poolHandle);
        }

        /// <summary>
        /// Encrypts a plaintext with the secret key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to:
        /// 1) in BFV, the highest (data) level in the modulus switching chain,
        /// 2) in CKKS, the encryption parameters of the plaintext.
        /// Dynamic memory allocations in the process are allocated from the memory
        /// pool pointed to by the given MemoryPoolHandle.
        ///
        /// Half of the polynomials in relinearization keys are randomly generated
        /// and are replaced with the seed used to compress output size. The output
        /// is in binary format and not human-readable. The output stream must have
        /// the "binary" flag set.
        /// </remarks>
        /// <param name="plain">The plaintext to encrypt</param>
        /// <param name="stream">The stream to save the Ciphertext to</param>
        /// <param name="comprMode">The desired compression mode</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if plain or stream is null</exception>
        /// <exception cref="ArgumentException">if plain is not valid for the encryption parameters</exception>
        /// <exception cref="ArgumentException">if plain is not in default NTT form</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public long EncryptSymmetricSave(Plaintext plain, Stream stream, ComprModeType? comprMode = null, MemoryPoolHandle pool = null)
        {
            if (null == plain)
                throw new ArgumentNullException(nameof(plain));
            if (null == stream)
                throw new ArgumentNullException(nameof(stream));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            Ciphertext destination = new Ciphertext(poolHandle);
            NativeMethods.Encryptor_EncryptSymmetric(NativePtr, plain.NativePtr, true, destination.NativePtr, poolHandle);
            return destination.Save(stream, comprMode);
        }

        /// <summary>
        /// Encrypts a zero plaintext with the secret key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to the given
        /// parms_id. Dynamic memory allocations in the process are allocated from the memory
        /// pool pointed to by the given MemoryPoolHandle.
        ///
        /// Half of the polynomials in relinearization keys are randomly generated
        /// and are replaced with the seed used to compress output size. The output
        /// is in binary format and not human-readable. The output stream must have
        /// the "binary" flag set.
        /// </remarks>
        /// <param name="parmsId">The ParmsId for the resulting ciphertext</param>
        /// <param name="stream">The stream to save the Ciphertext to</param>
        /// <param name="comprMode">The desired compression mode</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if parmsId or stream is null</exception>
        /// <exception cref="ArgumentException">if parmsId is not valid for the encryption parameters</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public long EncryptZeroSymmetricSave(ParmsId parmsId, Stream stream, ComprModeType? comprMode = null, MemoryPoolHandle pool = null)
        {
            if (null == parmsId)
                throw new ArgumentNullException(nameof(parmsId));
            if (null == stream)
                throw new ArgumentNullException(nameof(stream));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            Ciphertext destination = new Ciphertext(pool);
            NativeMethods.Encryptor_EncryptZeroSymmetric1(NativePtr, parmsId.Block, true, destination.NativePtr, poolHandle);
            return destination.Save(stream, comprMode);
        }

        /// <summary>
        /// Encrypts a zero plaintext with the secret key and stores the result in destination.
        /// </summary>
        /// <remarks>
        /// The encryption parameters for the resulting ciphertext correspond to the
        /// highest (data) level in the modulus switching chain. Dynamic memory allocations in
        /// the process are allocated from the memory pool pointed to by the given MemoryPoolHandle.
        ///
        /// Half of the polynomials in relinearization keys are randomly generated
        /// and are replaced with the seed used to compress output size. The output
        /// is in binary format and not human-readable. The output stream must have
        /// the "binary" flag set.
        /// </remarks>
        /// <param name="stream">The stream to save the Ciphertext to</param>
        /// <param name="comprMode">The desired compression mode</param>
        /// <param name="pool">The MemoryPoolHandle pointing to a valid memory pool</param>
        /// <exception cref="ArgumentNullException">if stream is null</exception>
        /// <exception cref="ArgumentException">if pool is uninitialized</exception>
        public long EncryptZeroSymmetricSave(Stream stream, ComprModeType? comprMode = null, MemoryPoolHandle pool = null)
        {
            if (null == stream)
                throw new ArgumentNullException(nameof(stream));

            IntPtr poolHandle = pool?.NativePtr ?? IntPtr.Zero;
            Ciphertext destination = new Ciphertext(pool);
            NativeMethods.Encryptor_EncryptZeroSymmetric2(NativePtr, true, destination.NativePtr, poolHandle);
            return destination.Save(stream, comprMode);
        }

        /// <summary>
        /// Destroy native object.
        /// </summary>
        protected override void DestroyNativeObject()
        {
            NativeMethods.Encryptor_Destroy(NativePtr);
        }
    }
}
