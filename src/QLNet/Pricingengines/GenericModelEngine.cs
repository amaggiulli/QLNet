﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

namespace QLNet
{

   public class GenericModelEngine<ModelType, ArgumentsType, ResultsType>
      : GenericEngine<ArgumentsType, ResultsType>
        where ArgumentsType : IPricingEngineArguments, new ()
        where ResultsType : IPricingEngineResults, new ()
           where ModelType : class,IObservable
   {
      public GenericModelEngine() { }
      public GenericModelEngine(Handle<ModelType> model)
      {
         model_ = model;
         model_.registerWith(update);
      }
      public GenericModelEngine(ModelType model)
      {
         model_  = new Handle<ModelType>(model);
         model_.registerWith(update);
      }
      public void setModel(Handle<ModelType> model)
      {
         if (model_ != null)
            model_.unregisterWith(update);
         model_ = model;
         if (model_ != null)
            model_.registerWith(update);
         update();
      }

      protected Handle<ModelType> model_;


   }
}
