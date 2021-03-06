﻿namespace SecureRemotePassword
{
	/// <summary>
	/// Session key and proof values generated by SRP-6a protocol.
	/// </summary>
	public class SrpSession
	{
		/// <summary>
		/// Gets or sets the session key.
		/// </summary>
		public byte[] Key { get; set; }

		/// <summary>
		/// Gets or sets the session proof.
		/// </summary>
		public byte[] Proof { get; set; }
	}
}
