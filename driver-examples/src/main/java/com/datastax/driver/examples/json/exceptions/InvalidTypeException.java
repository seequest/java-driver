package com.datastax.driver.examples.json.exceptions;

public class InvalidTypeException extends RuntimeException {

  public InvalidTypeException(String message, Throwable e) {
    super(message, e);
  }

  public InvalidTypeException(String msg) {
    super(msg);
  }
}
