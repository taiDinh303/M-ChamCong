import axiosClient from "../../../services/api/axiosClient";

const employeeApi = {
    getAll(pageNumber = 1, pageSize = 5) {
        return axiosClient.get("/Employee/get-all", {
            params: {
                pageNumber,
                pageSize,
            },
        });
    },

    getById(id) {
        return axiosClient.get(`/Employee/get-by-id/${id}`);
    },

    create(data) {
        return axiosClient.post("/Employee/create", data);
    },

    update(data) {
        return axiosClient.put("/Employee/update", data);
    },

    softDelete(id) {
        return axiosClient.delete(`/Employee/soft-delete/${id}`);
    },

    delete(id) {
        return axiosClient.delete(`/Employee/delete/${id}`);
    },
};

export default employeeApi;