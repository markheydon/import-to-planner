# FluentDataGrid - Advanced Patterns

## Basic Grid

```razor
<FluentDataGrid Items="@people">
    <PropertyColumn Property="@(p => p.Name)" Sortable="true" />
    <PropertyColumn Property="@(p => p.Email)" />
</FluentDataGrid>
```

## Pagination

```razor
<FluentDataGrid Items="@people" Pagination="@pagination">
    <PropertyColumn Property="@(p => p.Name)" Sortable="true" />
</FluentDataGrid>
<FluentPaginator State="@pagination" />
```

## Virtualisation

```razor
<FluentDataGrid Items="@people" Virtualize="true" ItemSize="46" OverscanCount="5" style="height: 500px; overflow-y: auto;">
    <PropertyColumn Property="@(p => p.Name)" />
</FluentDataGrid>
```

## ItemsProvider (Remote Data)

```razor
<FluentDataGrid ItemsProvider="@dataProvider" Virtualize="true" ItemSize="46">
    <PropertyColumn Property="@(p => p.Name)" Sortable="true" />
</FluentDataGrid>
```

## EF Adapter

Package:

```bash
dotnet add package Microsoft.FluentUI.AspNetCore.Components.DataGrid.EntityFrameworkAdapter --prerelease
```

Registration:

```csharp
builder.Services.AddDataGridEntityFrameworkAdapter();
```

## Useful Patterns

- `TemplateColumn` for custom cell content.
- `ColumnOptions` for per-column filters.
- `ResizableColumns="true"` for drag resizing.
- `LoadingContent` and `EmptyContent` for UX states.
- `RefreshDataAsync()` for programmatic reload.
