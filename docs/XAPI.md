# XAPI-Contracts

XAPI-Contracts provides high performance API for contracts.

## Key features

- Working with contracts;

## QueryRoot

#### List of queries:

|#|Endpoint|Arguments|Return|
|------|---------|---------|---------|
|1|[contract](#contract)|`id`|Contract|
|2|[organizationContracts](#organizationContracts)|`organizationId` `storeId` `vendorId` `statuses` `codes` `startDate` `endDate`|Paginated organization contracts list|

### Contracts

```
{
    contract (id: "d97ee2c7-e29d-440a-a43a-388eb5586087")
    {
      id
      name
      description
      status
      storeId
      vendorId
      code
      startDate
      endDate
      dynamicProperties { name value valueType }
    }
}
```

### Contracts connection

With this connection you can get all organization's contracts.
```
{
    organizationContracts (
      storeId: "B2B-store"
      organizationId: "33ae18d0-b7ba-4844-8c19-afd49489401b"
      statuses: ["Draft", "Active"]
      vendorId: "478e51d0-b7ba-4334-8109-abb45512101b"
      after: 0
      first: 20)
    {
        items
        {
          id
          name
          description
          status
          storeId
          vendorId
          code
          startDate
          endDate
          dynamicProperties { name value valueType }
        }
        totalCount
    }
}
```
