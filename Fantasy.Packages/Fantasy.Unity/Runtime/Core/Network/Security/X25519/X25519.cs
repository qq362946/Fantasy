/* 
BSD 3-Clause License

Copyright (c) 2020, Hirbod Behnam
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Security.Cryptography;

// RNGCryptoServiceProvider 在 .NET 6+ 上标记为 obsolete，
// 但 Unity 需要它作为兼容 fallback（RandomNumberGenerator.Fill 在部分版本缺失）
#pragma warning disable SYSLIB0023

namespace Fantasy.Network.Security
{
    public static class Curve25519
    {
        /// <summary>
        /// The base point that is x = 9
        /// </summary>
        public static readonly byte[] Basepoint = {9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        /// <summary>
        /// An inner function to calculate scalar * point
        /// </summary>
        /// <param name="input"></param>
        /// <param name="baseIn"></param>
        /// <returns></returns>
        private static byte[] ScalarMult(byte[] input, byte[] baseIn)
        {
            var e = new byte[32];
            
            Array.Copy(input,e,32); //copy(e[:], input[:])
            e[0] &= 248;
            e[31] &= 127;
            e[31] |= 64;

            FieldElement x1, x2, z2, x3, z3, tmp0, tmp1;
            z2 = new FieldElement();
            // feFromBytes(&x1, base)
            x1 = new FieldElement(baseIn); //SECOND NUMBER
            //feOne(&x2)
            x2 = new FieldElement();
            x2.One(); 
            //feCopy(&x3, &x1)
            x3 = new FieldElement();
            FieldElement.Copy(ref x3,x1);
            //feOne(&z3)
            z3 = new FieldElement();
            z3.One();

            int swap = 0;
            for (int pos = 254; pos >= 0; pos--) {
                byte b = Convert.ToByte(e[pos / 8] >> (pos & 7));
                b &= 1;
                swap ^= (int)(b);
                FieldElement.CSwap(ref x2, ref x3, swap);
                FieldElement.CSwap(ref z2, ref z3, swap);
                swap = (int) (b);

                tmp0 = x3 - z3; //feSub(&tmp0, &x3, &z3)
                tmp1 = x2 - z2; //feSub(&tmp1, &x2, &z2)
                x2 += z2; //feAdd(&x2, &x2, &z2)
                z2 = x3 + z3; //feAdd(&z2, &x3, &z3)
                z3 = tmp0.Multiply(x2);
                z2 = z2.Multiply(tmp1);
                tmp0 = tmp1.Square();
                tmp1 = x2.Square();
                x3 = z3 + z2; //feAdd(&x3, &z3, &z2)
                z2 = z3 - z2; //feSub(&z2, &z3, &z2)
                x2 = tmp1.Multiply(tmp0);
                tmp1 -= tmp0;//feSub(&tmp1, &tmp1, &tmp0)
                z2 = z2.Square();
                z3 = tmp1.Mul121666();
                x3 = x3.Square();
                tmp0 += z3; //feAdd(&tmp0, &tmp0, &z3)
                z3 = x1.Multiply(z2);
                z2 = tmp1.Multiply(tmp0);
            }

            FieldElement.CSwap(ref x2, ref x3, swap);
            FieldElement.CSwap(ref z2, ref z3, swap);

            z2 = z2.Invert();
            x2 = x2.Multiply(z2);
            return x2.ToBytes();
        }
        /// <summary>
        /// X25519 returns the result of the scalar multiplication (scalar * point),
        /// according to RFC 7748, Section 5. scalar, point and the return value are
        /// slices of 32 bytes.
        ///
        /// If point is Basepoint (but not if it's a different slice with the same
        /// contents) a precomputed implementation might be used for performance.
        /// </summary>
        /// <returns></returns>
        public static byte[] ScalarMultiplication(byte[] scalar,byte[] point)
        {
            if (scalar.Length != 32)
                throw new ArgumentException("Length of scalar must be 32",nameof(scalar));
            if (point.Length != 32)
                throw new ArgumentException("Length of point must be 32",nameof(point));
            byte[] zero = new byte[32];
            byte[] result = ScalarMult(scalar, point);
            // here I tried to make something like subtle.ConstantTimeCompare
            if (result.Length != zero.Length)
                throw new Exception("This should not happen. Because result is always 32 bytes");

            byte v = 0;
            for (int i = 0; i < result.Length; i++)
                v = (byte)(v | (zero[i] ^ result[i]));
            if ((int)(((uint)(v^0) - 1) >> 31) == 1) // no clue if these functions are ok or not
                throw new Exception("bad input point: low order point");
            return result;
        }
    }

    /// <summary>
    /// A friendly front-end for you developers to generate keys and calculate agreements
    /// </summary>
    public static class X25519KeyAgreement
    {
        /// <summary>
        /// Uses RNG Random to generate a key pair
        /// </summary>
        /// <returns>A random key pair</returns>
        public static X25519KeyPair GenerateKeyPair()
        {
            // at first generate the private key
            X25519KeyPair key = new X25519KeyPair
            {
                PrivateKey = new byte[32]
            };
            using (var rnd = new RNGCryptoServiceProvider())
                rnd.GetBytes(key.PrivateKey);
            // as defined in https://cr.yp.to/ecdh.html do these operation to finalize the private key
            key.PrivateKey[0] &= 248;
            key.PrivateKey[31] &= 127;
            key.PrivateKey[31] |= 64;
            // compute the public key
            key.PublicKey = Curve25519.ScalarMultiplication(key.PrivateKey, Curve25519.Basepoint);
            return key;
        }

        /// <summary>
        /// Generates a full key pair (Public and private) given the private key
        /// </summary>
        /// <param name="privateKey">The private key to generate public key from</param>
        /// <returns>A full key pair</returns>
        public static X25519KeyPair GenerateKeyFromPrivateKey(byte[] privateKey)
        {
            X25519KeyPair key = new X25519KeyPair
            {
                PrivateKey = privateKey
            };
            key.PublicKey = Curve25519.ScalarMultiplication(key.PrivateKey, Curve25519.Basepoint);
            return key;
        }
        /// <summary>
        /// Generate a shared secret with the other users public key and your private key
        /// </summary>
        /// <param name="myPrivateKey">Your private key</param>
        /// <param name="otherPublicKey">The public key of the other user</param>
        /// <returns>A shared secret</returns>
        public static byte[] Agreement(byte[] myPrivateKey, byte[] otherPublicKey)
        {
            return Curve25519.ScalarMultiplication(myPrivateKey, otherPublicKey);
        }
    }

    /// <summary>
    /// Private an public keys for Curve25519
    /// </summary>
    public struct X25519KeyPair
    {
        /// <summary>
        /// The private key that you have to keep secret
        /// </summary>
        public byte[] PrivateKey;
        /// <summary>
        /// The public key that you have to share with users
        /// </summary>
        public byte[] PublicKey;
    }
}
