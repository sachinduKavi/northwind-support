import debounce from "debounce";
import { useEffect, useMemo, useState } from "react";
import { ApiError } from "../../../api/client";
import { ticketsApi } from "../../../api/tickets";
import type {
  PagedResult,
  TicketFilters,
  TicketListItem,
} from "../../../types/api";

const DEBOUNCE_MS = 300;

export const emptyFilters: TicketFilters = {
  search: "",
  status: undefined,
  priority: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  unassignedOnly: false,
  slaStatus: undefined,
  page: 1,
  pageSize: 20,
  sortBy: "createdAtUtc",
  sortDirection: "desc",
};

interface Result {
  filters: TicketFilters;
  reloadCount: number;
  data?: PagedResult<TicketListItem>;
  error?: string;
}

/** Owns the ticket list's filters and the data they produce. */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [searchText, setSearchText] = useState(emptyFilters.search);
  const [reloadCount, setReloadCount] = useState(0);
  const [result, setResult] = useState<Result>();

  const applySearch = useMemo(
    () =>
      debounce((search: string) => {
        setFilters((current) =>
          current.search === search ? current : { ...current, search, page: 1 },
        );
      }, DEBOUNCE_MS),
    [],
  );

  useEffect(() => () => applySearch.clear(), [applySearch]);

  useEffect(() => {
    let isCurrent = true;

    ticketsApi.getTickets(filters).then(
      (data) => {
        if (isCurrent) setResult({ filters, reloadCount, data });
      },
      (error: unknown) => {
        if (!isCurrent) return;
        const message =
          error instanceof ApiError ? error.message : "Unable to load tickets.";
        setResult((current) => ({
          filters,
          reloadCount,
          data: current?.data,
          error: message,
        }));
      },
    );

    return () => {
      isCurrent = false;
    };
  }, [filters, reloadCount]);

  const updateFilters = (patch: Partial<TicketFilters>) => {
    const { search, ...rest } = patch;

    if (search !== undefined) {
      setSearchText(search);
      applySearch(search);
    }
    if (Object.keys(rest).length > 0) {
      setFilters((current) => ({ ...current, ...rest, page: rest.page ?? 1 }));
    }
  };

  const resetFilters = () => {
    applySearch.clear();
    setSearchText(emptyFilters.search);
    setFilters(emptyFilters);
  };

  const reload = () => setReloadCount((count) => count + 1);

  const isLoading =
    result?.filters !== filters || result.reloadCount !== reloadCount;

  return {
    filters: { ...filters, search: searchText },
    updateFilters,
    resetFilters,
    reload,
    isLoading,
    data: result?.data,
    error: isLoading ? undefined : result.error,
  };
}
