﻿////////////////////////////////////////////////////////////////////////////////
//
//  NullableDoubleToDoubleConverter.cs - This file is part of LibVLC.NET.
//
//    Copyright (C) 2011 Boris Richter <himself@boris-richter.net>
//
//  ==========================================================================
//  
//  LibVLC.NET is free software; you can redistribute it and/or modify it 
//  under the terms of the GNU Lesser General Public License as published by 
//  the Free Software Foundation; either version 2.1 of the License, or (at 
//  your option) any later version.
//    
//  LibVLC.NET is distributed in the hope that it will be useful, but WITHOUT 
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
//  FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public 
//  License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License 
//  along with LibVLC.NET; if not, see http://www.gnu.org/licenses/.
//
//  ==========================================================================
// 
//  $LastChangedRevision: 12546 $
//  $LastChangedDate: 2011-11-07 12:22:05 -0800 (Mon, 07 Nov 2011) $
//  $LastChangedBy: unknown $
//
////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace TestPresentation
{

  //****************************************************************************
  public class NullableDoubleToDoubleConverter
    : IValueConverter
  {

    //==========================================================================
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if(value == null)
        return 0.0;
      return (value as double?).Value;
    }

    //==========================================================================
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (double?)(double)value;
    }

  } // class NullableDoubleToDoubleConverter

}
