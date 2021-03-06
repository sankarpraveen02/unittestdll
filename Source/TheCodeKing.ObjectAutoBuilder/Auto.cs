﻿/*=============================================================================
*
*	(C) Copyright 2011, Michael Carlisle (mike.carlisle@thecodeking.co.uk)
*
*   http://www.TheCodeKing.co.uk
*  
*	All rights reserved.
*	The code and information is provided "as-is" without waranty of any kind,
*	either expressed or implied.
*
*=============================================================================
*/
using System;
using AutoObjectBuilder.Config;
using AutoObjectBuilder.Core;
using AutoObjectBuilder.Interfaces;

namespace AutoObjectBuilder
{
    public sealed class Auto
    {
        public static readonly DefaultAutoConfiguration Configure;

        static Auto()
        {
            Configure = new DefaultAutoConfiguration();
            Configure.UseDefaultConfiguration();
        }

        public static AutoExpression<T> Make<T>()
        {
            IObjectParser objParser = new ObjectParser();
            IAutoBuilder interfaceBuilder = new AutoProxyBuilder();
            Func<IAutoConfigurationResolver, IAutoBuilder, IObjectParser, IAutoFiller> fillerFactory =
                (config, builder, parser) => new AutoFiller(config, builder, parser);
            Func
                <IAutoConfigurationResolver, Func<IAutoConfigurationResolver, IAutoBuilder, IObjectParser, IAutoFiller>,
                    IAutoBuilder> builderFactory =
                        (config, filler) => new AutoBuilder(config, filler, objParser, interfaceBuilder);
            return new AutoExpression<T>(builderFactory, fillerFactory, Configure);
        }
    }
}