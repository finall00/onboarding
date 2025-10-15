import { useState, useEffect, useCallback } from "react";
import {
  Container,
  Typography,
  Box,
  Button,
  TextField,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  CircularProgress,
  InputAdornment,
  Toolbar,
  Stack,
  TablePagination,
} from "@mui/material";
import {
  Add as AddIcon,
  Search as SearchIcon,
  Visibility as VisibilityIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Refresh as RefreshIcon,
  Replay as ReplayIcon,
} from "@mui/icons-material";
import type { LeadList } from "./model/leadlist";
import { leadListService, leadListCache } from "./services/LeadListService";
import { StatusBadge } from "./components/StatusBadge";
import { CreateLeadListDialog } from "./components/CreateLeadListDialog";
import { EditLeadListDialog } from "./components/EditLeadListDialog";
import { DeleteConfirmDialog } from "./components/DeleteConfirmDialog";
import { LeadListDetailsDialog } from "./components/LeadListDetailsDialog";
import { Toast } from "./components/Toast";

function App() {
  const [leadLists, setLeadLists] = useState<LeadList[]>([]);
  const [totalCount, setTotalCount] = useState<number | undefined>(undefined);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(0);
  const defaultPageSize = Number(import.meta.env.VITE_PAGE_SIZE ?? 10) || 10;
  const [pageSize, setPageSize] = useState(defaultPageSize);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [searchQuery, setSearchQuery] = useState("");
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [isDetailsDialogOpen, setIsDetailsDialogOpen] = useState(false);
  const [selectedLeadList, setSelectedLeadList] = useState<LeadList | null>(
    null
  );

  const envPoll = Number(import.meta.env.VITE_POLL_MS ?? NaN);
  const poolingIntervalMs = Number.isFinite(envPoll) && envPoll > 0 ? envPoll : 60000;

  const [isDeleting, setIsDeleting] = useState(false);
  const [reprocessingIds, setReprocessingIds] = useState<string[]>([]);
  const [toast, setToast] = useState<{
    message: string;
    type: "success" | "error";
  } | null>(null);

  const fetchLeadLists = useCallback(async () => {
    try {
      const res = await leadListService.getAll({
        status: statusFilter || undefined,
        search: searchQuery || undefined,
        page: page + 1,
        pageSize,
      });
      setLeadLists(res.items);
      setTotalCount(res.total);
    } catch (error) {
      console.error(error);
      showToast("Failed to load lead lists", "error");
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter, searchQuery, page, pageSize]);

  useEffect(() => {
    fetchLeadLists();
  }, [fetchLeadLists]);

  useEffect(() => {
    const interval = setInterval(() => {
      fetchLeadLists();
    }, poolingIntervalMs);

    return () => clearInterval(interval);
  }, [fetchLeadLists, poolingIntervalMs]);

  useEffect(() => {
    setPage(0);
  }, [statusFilter, searchQuery]);

  const showToast = (message: string, type: "success" | "error") => {
    setToast({ message, type });
  };

  const handleCreate = async (data: { name: string; sourceUrl: string }) => {
    try {
      await leadListService.create(data);
      showToast("Lead list created successfully", "success");
      fetchLeadLists();
    } catch (error) {
      console.error("Failed to create lead list:", error);
      showToast("Failed to create lead list", "error");
      throw error;
    }
  };

  const handleEdit = async (data: { name: string; sourceUrl: string }) => {
    if (!selectedLeadList) return;

    try {
      await leadListService.update(selectedLeadList.id, data);
      showToast("Lead list updated successfully", "success");
      fetchLeadLists();
    } catch (error) {
      console.error("Failed to update lead list:", error);
      showToast("Failed to update lead list", "error");
      throw error;
    }
  };

  const handleDelete = async () => {
    if (!selectedLeadList) return;

    setIsDeleting(true);
    try {
      await leadListService.delete(selectedLeadList.id);
      showToast("Lead list deleted successfully", "success");
      setIsDeleteDialogOpen(false);
      setSelectedLeadList(null);
      fetchLeadLists();
    } catch (error) {
      console.error("Failed to delete lead list:", error);
      showToast("Failed to delete lead list", "error");
    } finally {
      setIsDeleting(false);
    }
  };

  const openEditDialog = (leadList: LeadList) => {
    if (!leadListService.canEdit(leadList)) {
      showToast("Can only edit lists with Pending or Failed status", "error");
      return;
    }
    setSelectedLeadList(leadList);
    setIsEditDialogOpen(true);
  };

  const openDeleteDialog = (leadList: LeadList) => {
    if (!leadListService.canDelete(leadList)) {
      showToast("Can only delete lists with Pending or Failed status", "error");
      return;
    }
    setSelectedLeadList(leadList);
    setIsDeleteDialogOpen(true);
  };

  const openDetailsDialog = (leadList: LeadList) => {
    setSelectedLeadList(leadList);
    setIsDetailsDialogOpen(true);
  };

  const handleReprocess = async (id: string) => {
    if (!id) return;
    setReprocessingIds((prev) => [...prev, id]);
    try {
      await leadListService.reprocess(id);
      showToast("Reprocess started", "success");
      await fetchLeadLists();
    } catch (error) {
      console.error("Failed to reprocess lead list:", error);
      showToast("Failed to reprocess lead list", "error");
    } finally {
      setReprocessingIds((prev) => prev.filter((x) => x !== id));
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <Container maxWidth="xl" sx={{ py: 4 }}>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom fontWeight="bold">
          Lead Lists
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Lorem ipsum dolor sit amet consectetur adipisicing elit. At tempore fugiat id consequatur veniam vel veritatis corrupti libero, illo, eum harum ea ratione impedit rem, optio provident in deserunt. Fuga.
        </Typography>
      </Box>

      <Paper sx={{ mb: 3 }}>
        <Toolbar sx={{ gap: 2, flexWrap: "wrap" }}>
          <TextField
            placeholder="Search by name..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            size="small"
            sx={{ flex: { xs: "1 1 100%", sm: "1 1 auto" }, minWidth: 200 }}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
              },
            }}
          />

          <TextField
            select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            size="small"
            sx={{ minWidth: 150 }}
          >
            <MenuItem value="">All Statuses</MenuItem>
            <MenuItem value="Pending">Pending</MenuItem>
            <MenuItem value="Processing">Processing</MenuItem>
            <MenuItem value="Completed">Completed</MenuItem>
            <MenuItem value="Failed">Failed</MenuItem>
          </TextField>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
              <IconButton
              size="small"
              onClick={async () => {
                setIsLoading(true);
                try {
                  leadListCache.forceReload();
                  await fetchLeadLists();
                } finally {
                  setIsLoading(false);
                }
              }}
              title="Refresh"
              disabled={isLoading}
            >
              <RefreshIcon fontSize="small" />
            </IconButton>

            <Box sx={{ flex: 1 }} />

            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setIsCreateDialogOpen(true)}
            >
              Create List
            </Button>
          </Box>
        </Toolbar>
      </Paper>

      {isLoading ? (
        <Paper sx={{ p: 8, textAlign: "center" }}>
          <CircularProgress sx={{ mb: 2 }} />
          <Typography color="text.secondary">Loading lead lists...</Typography>
        </Paper>
      ) : leadLists.length === 0 ? (
        <Paper sx={{ p: 8, textAlign: "center" }}>
          <Typography color="text.secondary" gutterBottom>
            No lead lists found
          </Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setIsCreateDialogOpen(true)}
            sx={{ mt: 2 }}
          >
            Create Your First List
          </Button>
        </Paper>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Source URL</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Processed</TableCell>
                <TableCell>Created</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {leadLists.map((leadList) => (
                <TableRow key={leadList.id} hover onClick={() => openDetailsDialog(leadList)} sx={{ cursor: 'pointer' }}>
                  <TableCell>
                    <Typography variant="body2" fontWeight="medium">
                      {leadList.name.slice(0, 50)}{leadList.name.length > 50 ? "…" : ""}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{
                        maxWidth: 300,
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                      }}
                      title={leadList.sourceUrl}
                    >
                      {leadList.sourceUrl}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <StatusBadge status={leadList.status} />
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="body2">
                      {leadList.processedCount.toLocaleString()}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {formatDate(leadList.createdAt)}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Stack
                      direction="row"
                      spacing={0.5}
                      justifyContent="flex-end"
                    >
                      <IconButton
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          openDetailsDialog(leadList);
                        }}
                        color="primary"
                        title="View Details"
                      >
                        <VisibilityIcon fontSize="small" />
                      </IconButton>
                      {leadList.status === "Failed" && (
                        <IconButton
                          size="small"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleReprocess(leadList.id);
                          }}
                          title="Reprocess"
                        >
                          {reprocessingIds.includes(leadList.id) ? (
                            <CircularProgress size={18} />
                          ) : (
                            <ReplayIcon fontSize="small" />
                          )}
                        </IconButton>
                      )}
                      <IconButton
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          openEditDialog(leadList);
                        }}
                        disabled={!leadListService.canEdit(leadList)}
                        title={
                          leadListService.canEdit(leadList)
                            ? "Edit"
                            : "Cannot edit (only Pending/Failed)"
                        }
                      >
                        <EditIcon fontSize="small" />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          openDeleteDialog(leadList);
                        }}
                        disabled={!leadListService.canDelete(leadList)}
                        color="error"
                        title={
                          leadListService.canDelete(leadList)
                            ? "Delete"
                            : "Cannot delete (only Pending/Failed)"
                        }
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
            <Box sx={{ display: "flex", justifyContent: "flex-end", p: 1 }}>
              <TablePagination
                component="div"
                count={typeof totalCount === "number" ? totalCount : leadLists.length}
                page={page}
                onPageChange={(_event: unknown, newPage: number) => setPage(newPage)}
                rowsPerPage={pageSize}
                onRowsPerPageChange={(e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
                  setPageSize(Number(e.target.value));
                  setPage(0);
                }}
                rowsPerPageOptions={[5, 10, 25, 50]}
              />
            </Box>
        </TableContainer>
      )}

      <CreateLeadListDialog
        isOpen={isCreateDialogOpen}
        onClose={() => setIsCreateDialogOpen(false)}
        onSubmit={handleCreate}
      />

      <EditLeadListDialog
        isOpen={isEditDialogOpen}
        onClose={() => {
          setIsEditDialogOpen(false);
          setSelectedLeadList(null);
        }}
        onSubmit={handleEdit}
        leadList={selectedLeadList}
      />

      <DeleteConfirmDialog
        isOpen={isDeleteDialogOpen}
        onClose={() => {
          setIsDeleteDialogOpen(false);
          setSelectedLeadList(null);
        }}
        onConfirm={handleDelete}
        leadListName={selectedLeadList?.name || ""}
        isDeleting={isDeleting}
      />

      <LeadListDetailsDialog
        isOpen={isDetailsDialogOpen}
        onClose={() => {
          setIsDetailsDialogOpen(false);
          setSelectedLeadList(null);
        }}
        leadList={selectedLeadList}
      />

      {toast && (
        <Toast
          message={toast.message}
          type={toast.type}
          isVisible={!!toast}
          onClose={() => setToast(null)}
        />
      )}
    </Container>
  );
}

export default App;