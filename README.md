[![Build status](https://ci.appveyor.com/api/projects/status/kikmu5buw86b2pj0?svg=true)](https://ci.appveyor.com/project/GrumpyBusted/grumpy-messagequeue)
[![codecov](https://codecov.io/gh/GrumpyBusted/Grumpy.MessageQueue/branch/master/graph/badge.svg)](https://codecov.io/gh/GrumpyBusted/Grumpy.MessageQueue)
[![nuget](https://img.shields.io/nuget/v/Grumpy.MessageQueue.svg)](https://www.nuget.org/packages/Grumpy.MessageQueue/)
[![downloads](https://img.shields.io/nuget/dt/Grumpy.MessageQueue.svg)](https://www.nuget.org/packages/Grumpy.MessageQueue/)

# Grumpy.MessageQueue
API for Microsoft Message Queue (MSMQ).

This API extents the API with:
- Larger messages (Move the limit from 4MB to the size of a string 1-2GB)
- Transactional messages with Ack and NAck feature
- Queue Handler (Listener) to trigger handler method when messages are received
- Automatic management of queues (Create and Delete)
- Cancelable Receiver
- Async Receiver

This API includes the features that are most used (and needed for my other solution). The API makes the use
of MSMQ easy and strait forward. I know this library is opinionated and only provide a sub set of the features
in MSMQ API for Microsoft Message Queue (MSMQ).

## Send and Receive
The following code sample will:
- Create a new Private Queue on the current maschine, if not exists
- Open a connections to the queue
- Send a Message
- Receive the same message
- Close the connection
```csharp
var queueFactory = new QueueFactory();

using (var queue = queueFactory.CreateLocale("MyQueue", true, LocaleQueueMode.DurableCreate, true)) 
{
    // Message will be serialized to json and chumped into pieces, all pieces send in the same transaction
    queue.Send("MyMessage");

    // The receive has a timeout of 1 sec, and the message will be deserialized to the type (here string). This receive will automatically Ack the message
    var response = queue.Receive<string>(1000, new CancellationToken());
}
```

## Receive and Ack
The following code sample will:
- Receive a transactional message
- Try to Handle the message in some other method
- If any exception, NAck the message for next receive to handle the samme message
```csharp
var queueFactory = new QueueFactory();

using (var queue = queueFactory.CreateLocale("MyQueue", true, LocaleQueueMode.Durable, true)) 
{
    var message = queue.Receive(1000, new CancellationToken());

    try {
        Handle(messege.Message)
        message.Ack();
    }
    catch
    {
        message.NAck();
    }
}
```

## Queue Handler (Listerner)
The following code sample will:
- Create a Queue handler
- Start Listening to a Queue
- For each message call the method MessageHandler
- Ack and NAck are handled in the QueueHandler
```csharp
var queueFactory = new QueueFactory();
var queueHandlerFactory = new QueueHandlerFactory(queueFactory);

using (var queueHandler = queueHandlerFactory.Create()) 
{
    queueHandler.Start("MyQueue", true, LocaleQueueMode.Durable, true, MessageHandler, null, null, 0, true, false, new CancellationToken());

    // Wait for ever, or until cancelled
}

public void MessageHandler(object message, CancellationToken cancellationToken) 
{
    // Do stuff with message
}
```

Just ask for more samples!!!