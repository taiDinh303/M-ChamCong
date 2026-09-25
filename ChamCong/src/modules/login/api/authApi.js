import axiosClient from "../../../services/api/axiosClient";

const authApi = {
    login(data) {
        return axiosClient.post("/Auth/login", data);
    },
};

export default authApi;