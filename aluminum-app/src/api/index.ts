import http from '../utils/http'

// 认证
export const login = (data: { username: string; password: string }) =>
  http.post('/auth/login', data)

export const register = (data: { username: string; password: string; displayName?: string }) =>
  http.post('/auth/register', data)

// 订单
export const getOrders = (params: { page: number; pageSize: number; keyword?: string; customer?: string; status?: number | null }) =>
  http.get('/order', { params })

export const getOrderColumnValues = (column: string) =>
  http.get(`/order/column-values/${column}`)

export const createOrder = (data: any) => http.post('/order', data)
export const updateOrder = (data: any) => http.put('/order', data)
export const deleteOrder = (id: number) => http.delete(`/order/${id}`)
export const batchDeleteOrders = (ids: number[]) => http.post('/order/batch-delete', ids)
export const importOrders = (file: File) => {
  const fd = new FormData()
  fd.append('file', file)
  return http.post('/order/import', fd)
}
export const exportOrders = () =>
  http.get('/order/export', { responseType: 'blob' })
export const downloadOrderTemplate = () =>
  http.get('/order/template', { responseType: 'blob' })

// 库存
export const getInventory = (params: { page: number; pageSize: number; keyword?: string; materialName?: string; status?: number | null; spec?: string; materialType?: number | null }) =>
  http.get('/inventory', { params })

export const getInventoryColumnValues = (column: string) =>
  http.get(`/inventory/column-values/${column}`)

export const createInventory = (data: any) => http.post('/inventory', data)
export const updateInventory = (data: any) => http.put('/inventory', data)
export const deleteInventory = (id: number) => http.delete(`/inventory/${id}`)
export const importInventory = (file: File) => {
  const fd = new FormData()
  fd.append('file', file)
  return http.post('/inventory/import', fd)
}
export const exportInventory = () =>
  http.get('/inventory/export', { responseType: 'blob' })
export const downloadInventoryTemplate = () =>
  http.get('/inventory/template', { responseType: 'blob' })

// 废料设置
export const getWasteSettings = () => http.get('/wastesetting')
export const saveWasteSetting = (data: any) => http.post('/wastesetting', data)
export const deleteWasteSetting = (id: number) => http.delete(`/wastesetting/${id}`)

// 配料
export const calculateNesting = (data: { 
  orderIds: number[]
  calcMode?: string
  cutDirection?: string
  fixedDirection?: boolean
}) => http.post('/nesting/calculate', data)

export const confirmNesting = (data: any) =>
  http.post('/nesting/confirm', data)

export const getAvailableOrders = () =>
  http.get('/nesting/available-orders')

export const getNestingResults = (params: { page: number; pageSize: number; keyword?: string }) =>
  http.get('/nesting', { params })

// 品种
export const getVarieties = (params: any) => http.get('/variety', { params })
export const getVariety = (id: number) => http.get(`/variety/${id}`)
export const createVariety = (data: any) => http.post('/variety', data)
export const updateVariety = (data: any) => http.put('/variety', data)
export const deleteVariety = (id: number) => http.delete(`/variety/${id}`)
export const batchDeleteVarieties = (ids: number[]) => http.post('/variety/batch-delete', ids)
export const toggleVariety = (id: number) => http.post(`/variety/${id}/toggle`)
export const getVarietyFilterOptions = () => http.get('/variety/filter-options')
export const getAllVarieties = () => http.get('/variety/all')

// 配料结果(UsedList)
export const getUsedList = (params: { page: number; pageSize: number; keyword?: string }) =>
  http.get('/usedlist', { params })

// 用户管理
export const getUsers = (params: { page: number; pageSize: number; keyword?: string }) =>
  http.get('/user', { params })

export const createUser = (data: { userId: string; userName: string; tel?: string; password: string }) =>
  http.post('/user', data)

export const updateUser = (data: any) => http.put('/user', data)
export const deleteUser = (id: number) => http.delete(`/user/${id}`)
