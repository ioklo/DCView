package com.dcview.model;

import java.util.List;

public interface IPage<T>
{
    List<T> GetNext();
}
