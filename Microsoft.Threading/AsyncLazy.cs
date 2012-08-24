﻿//-----------------------------------------------------------------------
// <copyright file="AsyncLazy.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Threading {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// A thread-safe, lazily and asynchronously evaluated value factory.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class AsyncLazy<T> {
		/// <summary>
		/// The object to lock to provide thread-safety.
		/// </summary>
		private readonly object syncObject = new object();

		/// <summary>
		/// The function to invoke to produce the task.
		/// </summary>
		private Func<Task<T>> valueFactory;

		/// <summary>
		/// The result of the value factory.
		/// </summary>
		private Task<T> value;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class.
		/// </summary>
		/// <param name="valueFactory">The async function that produces the value.  To be invoked at most once.</param>
		public AsyncLazy(Func<Task<T>> valueFactory) {
			Requires.NotNull(valueFactory, "valueFactory");
			this.valueFactory = valueFactory;
		}

		/// <summary>
		/// Gets a value indicating whether the value factory has been invoked.
		/// </summary>
		public bool IsValueCreated {
			get {
				Thread.MemoryBarrier();
				return this.valueFactory == null;
			}
		}

		/// <summary>
		/// Gets the task that produces or has produced the value.
		/// </summary>
		/// <returns>A task whose result is the lazily constructed value.</returns>
		public Task<T> GetValueAsync() {
			if (this.value == null) {
				lock (this.syncObject) {
					// Note that if multiple threads hit GetValueAsync() before
					// the valueFactory has completed its synchronous execution,
					// the only one thread will execute the valueFactory while the
					// other threads synchronously block till the synchronous portion
					// has completed.
					if (this.value == null) {
						try {
							var valueFactory = this.valueFactory;
							this.valueFactory = null;
							this.value = valueFactory();
						} catch (Exception ex) {
							var tcs = new TaskCompletionSource<T>();
							tcs.SetException(ex);
							this.value = tcs.Task;
						}
					}
				}
			}

			return this.value;
		}
	}
}
