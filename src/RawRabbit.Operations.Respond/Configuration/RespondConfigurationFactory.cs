﻿using System;
using RawRabbit.Configuration.Consumer;

namespace RawRabbit.Operations.Respond.Configuration
{
	public interface IRespondConfigurationFactory
	{
		RespondConfiguration Create<TRequest, TResponse>();
		RespondConfiguration Create(Type requestType, Type respondType);
	}

	public class RespondConfigurationFactory : IRespondConfigurationFactory
	{
		private readonly IConsumerConfigurationFactory _consumerFactory;

		public RespondConfigurationFactory(IConsumerConfigurationFactory consumerFactory)
		{
			this._consumerFactory = consumerFactory;
		}

		public RespondConfiguration Create<TRequest, TResponse>()
		{
			return this.Create(typeof(TRequest), typeof(TResponse));
		}

		public RespondConfiguration Create(Type requestType, Type respondType)
		{
			ConsumerConfiguration consumerCfg = this._consumerFactory.Create(requestType);
			return new RespondConfiguration
			{
				Queue = consumerCfg.Queue,
				Exchange = consumerCfg.Exchange,
				Consume = consumerCfg.Consume
			};
		}
	}
}
